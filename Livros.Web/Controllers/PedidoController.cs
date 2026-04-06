using Livros.Domain;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Livros.Web.Controllers {
    public class PedidoController : Controller {
        private readonly AppDbContext _context;
        private readonly LivroService _livroService;
        private readonly EnderecoService _enderecoService;

        public PedidoController(AppDbContext context, LivroService livroService, EnderecoService enderecoService) {
            _context = context;
            _livroService = livroService;
            _enderecoService = enderecoService;
        }

        public IActionResult Checkout(int id, int quantidade = 1) {
            var clienteId = ObterClienteId();

            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new {
                    returnUrl = Url.Action("Checkout", "Pedido", new { id = id, quantidade = quantidade })
                });
            }

            var vm = MontarCheckoutViewModel(clienteId.Value, id, new CheckoutFormData {
                LivroId = id,
                Quantidade = quantidade > 0 ? quantidade : 1
            });

            if (vm.Livro == null) {
                return NotFound();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FinalizarPedido(CheckoutFormData form) {
            var clienteId = ObterClienteId();

            if (clienteId == null) {
                return RedirectToAction("Login", "Auth");
            }

            var livro = _livroService.ObterPorId(form.LivroId);

            if (livro == null) {
                return NotFound();
            }

            form.Quantidade = form.Quantidade <= 0 ? 1 : form.Quantidade;

            var estoque = _context.Estoques.FirstOrDefault(e => e.LivroId == form.LivroId);
            if (estoque == null || estoque.Quantidade < form.Quantidade) {
                ModelState.AddModelError(string.Empty, "Quantidade indisponível em estoque para concluir a compra.");
                return View("Checkout", MontarCheckoutViewModel(clienteId.Value, form.LivroId, form));
            }

            var subtotal = livro.Preco * form.Quantidade;
            var frete = CalcularFrete(form.Quantidade);
            var desconto = CalcularDesconto(form.Cupom, subtotal);
            var total = subtotal + frete - desconto;

            ValidarPagamentos(clienteId.Value, form, total);
            if (!ModelState.IsValid) {
                return View("Checkout", MontarCheckoutViewModel(clienteId.Value, form.LivroId, form));
            }

            using var transaction = _context.Database.BeginTransaction();

            var enderecoId = ResolverEndereco(clienteId.Value, form);
            if (enderecoId == null) {
                transaction.Rollback();
                return View("Checkout", MontarCheckoutViewModel(clienteId.Value, form.LivroId, form));
            }

            try {
                var pedido = new Pedido {
                    ClienteId = clienteId.Value,
                    EnderecoId = enderecoId.Value,
                    Total = total,
                    Status = "EM PROCESSAMENTO",
                    Itens = new List<PedidoItem>(),
                    Pagamentos = new List<Pagamento>()
                };

                pedido.Itens.Add(new PedidoItem {
                    LivroId = livro.Id,
                    Quantidade = form.Quantidade,
                    PrecoUnitario = livro.Preco
                });

                AdicionarPagamentoAoPedido(clienteId.Value, form.Metodo1, form.Valor1, form.CartaoId1, form.SalvarNovoCartao1,
                    form.NomeCartao1, form.NumeroCartao1, form.Validade1, form.CVV1, pedido);

                if (!string.IsNullOrWhiteSpace(form.Metodo2)) {
                    AdicionarPagamentoAoPedido(clienteId.Value, form.Metodo2, form.Valor2, form.CartaoId2, form.SalvarNovoCartao2,
                        form.NomeCartao2, form.NumeroCartao2, form.Validade2, form.CVV2, pedido);
                }

                _context.Pedidos.Add(pedido);
                _context.SaveChanges();
                transaction.Commit();

                TempData["Sucesso"] = "Pedido finalizado com sucesso! Status inicial: EM PROCESSAMENTO.";
                return RedirectToAction("AreaCliente", "Cliente");
            }
            catch {
                transaction.Rollback();
                ModelState.AddModelError(string.Empty, "Não foi possível finalizar o pedido. Tente novamente.");
                return View("Checkout", MontarCheckoutViewModel(clienteId.Value, form.LivroId, form));
            }
        }

        private CheckoutViewModel MontarCheckoutViewModel(int clienteId, int livroId, CheckoutFormData form) {
            var livro = _livroService.ObterPorId(livroId);
            var enderecos = _enderecoService.ListarPorCliente(clienteId) ?? new List<Endereco>();
            var cartoes = _context.Cartoes
                .Where(c => c.ClienteId == clienteId)
                .OrderByDescending(c => c.IsPadrao)
                .ToList();

            if (form.EnderecoId == 0 && enderecos.Any() && string.IsNullOrWhiteSpace(form.Logradouro)) {
                var enderecoPadrao = enderecos.FirstOrDefault(e => e.IsPadrao) ?? enderecos.First();
                form.EnderecoId = enderecoPadrao.Id;
            }

            var subtotal = livro != null ? livro.Preco * form.Quantidade : 0;
            var frete = CalcularFrete(form.Quantidade);
            var desconto = CalcularDesconto(form.Cupom, subtotal);

            return new CheckoutViewModel {
                Livro = livro,
                Enderecos = enderecos,
                Cartoes = cartoes,
                Quantidade = form.Quantidade,
                Subtotal = subtotal,
                Frete = frete,
                Desconto = desconto,
                Total = subtotal + frete - desconto,
                Form = form
            };
        }

        private int? ResolverEndereco(int clienteId, CheckoutFormData form) {
            if (form.EnderecoId > 0) {
                var enderecoExistente = _context.Enderecos
                    .FirstOrDefault(e => e.Id == form.EnderecoId && e.ClienteId == clienteId);

                if (enderecoExistente == null) {
                    ModelState.AddModelError(string.Empty, "Selecione um endereço de entrega válido.");
                    return null;
                }

                return enderecoExistente.Id;
            }

            if (string.IsNullOrWhiteSpace(form.CEP) ||
                string.IsNullOrWhiteSpace(form.Logradouro) ||
                string.IsNullOrWhiteSpace(form.Numero) ||
                string.IsNullOrWhiteSpace(form.Bairro) ||
                string.IsNullOrWhiteSpace(form.Cidade) ||
                string.IsNullOrWhiteSpace(form.Estado)) {
                ModelState.AddModelError(string.Empty, "Preencha todos os campos obrigatórios do novo endereço.");
                return null;
            }

            var estadoSigla = form.Estado.Trim().ToUpper();
            var estadoEntity = _context.Estados.FirstOrDefault(e => e.Sigla == estadoSigla);
            if (estadoEntity == null) {
                estadoEntity = new Estado {
                    Nome = estadoSigla,
                    Sigla = estadoSigla
                };
                _context.Estados.Add(estadoEntity);
                _context.SaveChanges();
            }

            var cidadeNome = form.Cidade.Trim();
            var cidadeEntity = _context.Cidades.FirstOrDefault(c => c.Nome == cidadeNome && c.EstadoId == estadoEntity.Id);
            if (cidadeEntity == null) {
                cidadeEntity = new Cidade {
                    Nome = cidadeNome,
                    EstadoId = estadoEntity.Id
                };
                _context.Cidades.Add(cidadeEntity);
                _context.SaveChanges();
            }

            var bairroNome = form.Bairro.Trim();
            var bairroEntity = _context.Bairros.FirstOrDefault(b => b.Nome == bairroNome && b.CidadeId == cidadeEntity.Id);
            if (bairroEntity == null) {
                bairroEntity = new Bairro {
                    Nome = bairroNome,
                    CidadeId = cidadeEntity.Id
                };
                _context.Bairros.Add(bairroEntity);
                _context.SaveChanges();
            }

            var endereco = new Endereco {
                NomeEndereco = string.IsNullOrWhiteSpace(form.NomeEndereco) ? "Novo Endereço" : form.NomeEndereco.Trim(),
                CEP = form.CEP.Trim(),
                Logradouro = form.Logradouro.Trim(),
                Numero = form.Numero.Trim(),
                Complemento = form.Complemento?.Trim(),
                BairroId = bairroEntity.Id,
                CidadeId = cidadeEntity.Id,
                ClienteId = clienteId
            };

            _context.Enderecos.Add(endereco);
            _context.SaveChanges();
            return endereco.Id;
        }

        private void ValidarPagamentos(int clienteId, CheckoutFormData form, decimal total) {
            if (string.IsNullOrWhiteSpace(form.Metodo1)) {
                ModelState.AddModelError(string.Empty, "Selecione pelo menos uma forma de pagamento.");
                return;
            }

            var valor1 = form.Valor1 ?? 0;
            var valor2 = string.IsNullOrWhiteSpace(form.Metodo2) ? 0 : form.Valor2 ?? 0;
            var soma = decimal.Round(valor1 + valor2, 2);
            var totalArredondado = decimal.Round(total, 2);

            if (valor1 <= 0) {
                ModelState.AddModelError(string.Empty, "Informe um valor válido para o pagamento 1.");
            }

            if (!string.IsNullOrWhiteSpace(form.Metodo2) && valor2 <= 0) {
                ModelState.AddModelError(string.Empty, "Informe um valor válido para o pagamento 2.");
            }

            if (Math.Abs(soma - totalArredondado) > 0.01m) {
                ModelState.AddModelError(string.Empty, "A soma dos pagamentos deve ser igual ao total do pedido.");
            }

            var usaCupom = CalcularDesconto(form.Cupom, total) > 0;

            ValidarPagamentoCartao(clienteId, form.Metodo1, valor1, form.CartaoId1, form.NomeCartao1, form.NumeroCartao1, form.Validade1, form.CVV1, usaCupom, 1);

            if (!string.IsNullOrWhiteSpace(form.Metodo2)) {
                ValidarPagamentoCartao(clienteId, form.Metodo2, valor2, form.CartaoId2, form.NomeCartao2, form.NumeroCartao2, form.Validade2, form.CVV2, usaCupom, 2);
            }
        }

        private void ValidarPagamentoCartao(int clienteId, string metodo, decimal valor, int? cartaoId,
            string nome, string numero, string validade, string cvv, bool usaCupom, int indice) {
            if (!string.Equals(metodo, "cartao", StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            if (!usaCupom && valor < 10) {
                ModelState.AddModelError(string.Empty, $"O pagamento {indice} com cartão deve ter valor mínimo de R$ 10,00.");
            }

            if (cartaoId.HasValue && cartaoId.Value > 0) {
                var cartaoExistente = _context.Cartoes.FirstOrDefault(c => c.Id == cartaoId.Value && c.ClienteId == clienteId);
                if (cartaoExistente == null) {
                    ModelState.AddModelError(string.Empty, $"Selecione um cartão válido no pagamento {indice}.");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(numero) || string.IsNullOrWhiteSpace(validade) || string.IsNullOrWhiteSpace(cvv)) {
                ModelState.AddModelError(string.Empty, $"Preencha os dados completos do novo cartão no pagamento {indice}.");
            }
        }

        private void AdicionarPagamentoAoPedido(int clienteId, string metodo, decimal? valor, int? cartaoId, bool salvarNovoCartao,
            string nomeCartao, string numeroCartao, string validade, string cvv, Pedido pedido) {
            if (string.IsNullOrWhiteSpace(metodo) || !valor.HasValue || valor.Value <= 0) {
                return;
            }

            if (string.Equals(metodo, "cartao", StringComparison.OrdinalIgnoreCase) && (!cartaoId.HasValue || cartaoId.Value == 0) && salvarNovoCartao) {
                var novoCartao = new Cartao {
                    ClienteId = clienteId,
                    NomeImpresso = nomeCartao.Trim(),
                    Numero = numeroCartao.Trim(),
                    Validade = validade.Trim(),
                    CVV = cvv.Trim()
                };

                _context.Cartoes.Add(novoCartao);
            }

            pedido.Pagamentos.Add(new Pagamento {
                Metodo = metodo.Trim().ToLower(),
                Valor = valor.Value,
                Status = "Pendente"
            });
        }

        private decimal CalcularFrete(int quantidade) {
            if (quantidade <= 0) {
                quantidade = 1;
            }

            return 15 + Math.Max(quantidade - 1, 0) * 2;
        }

        private decimal CalcularDesconto(string cupom, decimal subtotal) {
            if (string.IsNullOrWhiteSpace(cupom)) {
                return 0;
            }

            return string.Equals(cupom.Trim(), "DESCONTO10", StringComparison.OrdinalIgnoreCase)
                ? decimal.Round(subtotal * 0.10m, 2)
                : 0;
        }

        private decimal ObterValorPagamento(string campo, decimal? valorPadrao) {
            if (Request?.Form == null || !Request.Form.ContainsKey(campo)) {
                return valorPadrao ?? 0;
            }

            var valorBruto = Request.Form[campo].ToString();
            if (string.IsNullOrWhiteSpace(valorBruto)) {
                return valorPadrao ?? 0;
            }

            if (decimal.TryParse(valorBruto, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var valorPtBr)) {
                return valorPtBr;
            }

            if (decimal.TryParse(valorBruto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorInvariant)) {
                return valorInvariant;
            }

            return valorPadrao ?? 0;
        }
        private int? ObterClienteId() {
            var clienteIdStr = HttpContext.Session.GetString("ClienteId");
            if (string.IsNullOrWhiteSpace(clienteIdStr)) {
                return null;
            }

            return int.Parse(clienteIdStr);
        }
    }
}

