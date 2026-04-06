using Livros.Domain;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Services;
using Livros.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace Livros.Web.Controllers {
    public class PedidoController : Controller {
        private const string CarrinhoSessionKey = "Carrinho";

        private readonly AppDbContext _context;
        private readonly LivroService _livroService;
        private readonly EnderecoService _enderecoService;

        public PedidoController(AppDbContext context, LivroService livroService, EnderecoService enderecoService) {
            _context = context;
            _livroService = livroService;
            _enderecoService = enderecoService;
        }

        [HttpGet]
        public IActionResult Carrinho() {
            var vm = MontarCarrinhoViewModel();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdicionarAoCarrinho(int livroId, int quantidade = 1) {
            var livro = _livroService.ObterPorId(livroId);
            if (livro == null || !livro.IsAtivo) {
                TempData["ErroCarrinho"] = "Não foi possível adicionar este livro ao carrinho.";
                return RedirectToAction("Index", "Home");
            }

            var estoque = _context.Estoques.FirstOrDefault(e => e.LivroId == livroId);
            if (estoque == null || estoque.Quantidade <= 0) {
                TempData["ErroCarrinho"] = "Este livro está sem estoque no momento.";
                return RedirectToAction("Detalhes", "Home", new { id = livroId });
            }

            var carrinho = ObterCarrinhoDaSessao();
            var itemExistente = carrinho.FirstOrDefault(i => i.LivroId == livroId);
            var quantidadeSolicitada = quantidade <= 0 ? 1 : quantidade;
            var quantidadeAtual = itemExistente?.Quantidade ?? 0;
            var quantidadeFinal = Math.Min(quantidadeAtual + quantidadeSolicitada, estoque.Quantidade);

            if (itemExistente == null) {
                carrinho.Add(new CarrinhoSessionItem {
                    LivroId = livroId,
                    Quantidade = quantidadeFinal
                });
            }
            else {
                itemExistente.Quantidade = quantidadeFinal;
            }

            SalvarCarrinhoNaSessao(carrinho);
            TempData["SucessoCarrinho"] = $"\"{livro.Titulo}\" foi adicionado ao carrinho.";

            return RedirectToAction("Carrinho");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AtualizarCarrinho(int livroId, int quantidade) {
            var carrinho = ObterCarrinhoDaSessao();
            var item = carrinho.FirstOrDefault(i => i.LivroId == livroId);

            if (item == null) {
                return RedirectToAction("Carrinho");
            }

            if (quantidade <= 0) {
                carrinho.Remove(item);
                SalvarCarrinhoNaSessao(carrinho);
                TempData["SucessoCarrinho"] = "Item removido do carrinho.";
                return RedirectToAction("Carrinho");
            }

            var estoque = _context.Estoques.FirstOrDefault(e => e.LivroId == livroId);
            if (estoque == null || estoque.Quantidade <= 0) {
                carrinho.Remove(item);
                SalvarCarrinhoNaSessao(carrinho);
                TempData["ErroCarrinho"] = "Este livro ficou indisponível e foi removido do carrinho.";
                return RedirectToAction("Carrinho");
            }

            item.Quantidade = Math.Min(quantidade, estoque.Quantidade);
            SalvarCarrinhoNaSessao(carrinho);

            if (item.Quantidade < quantidade) {
                TempData["ErroCarrinho"] = "A quantidade foi ajustada para o máximo disponível em estoque.";
            }
            else {
                TempData["SucessoCarrinho"] = "Carrinho atualizado com sucesso.";
            }

            return RedirectToAction("Carrinho");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoverDoCarrinho(int livroId) {
            var carrinho = ObterCarrinhoDaSessao();
            var item = carrinho.FirstOrDefault(i => i.LivroId == livroId);

            if (item != null) {
                carrinho.Remove(item);
                SalvarCarrinhoNaSessao(carrinho);
                TempData["SucessoCarrinho"] = "Item removido do carrinho.";
            }

            return RedirectToAction("Carrinho");
        }

        [HttpGet]
        public IActionResult CheckoutCarrinho() {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new {
                    returnUrl = Url.Action("CheckoutCarrinho", "Pedido")
                });
            }

            var itensCheckout = ObterItensCheckout(new CheckoutFormData { UsarCarrinho = true });
            if (!itensCheckout.Any()) {
                TempData["ErroCarrinho"] = "Seu carrinho está vazio para seguir ao checkout.";
                return RedirectToAction("Carrinho");
            }

            var form = new CheckoutFormData {
                UsarCarrinho = true,
                Quantidade = itensCheckout.Sum(i => i.Quantidade),
                LivroId = itensCheckout.First().Livro.Id
            };

            var vm = MontarCheckoutViewModel(clienteId.Value, form);
            return View("Checkout", vm);
        }

        public IActionResult Checkout(int id, int quantidade = 1) {
            var clienteId = ObterClienteId();

            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new {
                    returnUrl = Url.Action("Checkout", "Pedido", new { id = id, quantidade = quantidade })
                });
            }

            var form = new CheckoutFormData {
                LivroId = id,
                Quantidade = quantidade > 0 ? quantidade : 1,
                UsarCarrinho = false
            };

            var vm = MontarCheckoutViewModel(clienteId.Value, form);
            if (!vm.Itens.Any()) {
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

            var itensCheckout = ObterItensCheckout(form);
            if (!itensCheckout.Any()) {
                if (form.UsarCarrinho) {
                    TempData["ErroCarrinho"] = "Seu carrinho está vazio para finalizar a compra.";
                    return RedirectToAction("Carrinho");
                }

                return NotFound();
            }

            form.Quantidade = itensCheckout.Sum(i => i.Quantidade);
            form.LivroId = itensCheckout.First().Livro.Id;
            form.Valor1 = ObterValorPagamento("Valor1", form.Valor1);
            form.Valor2 = string.IsNullOrWhiteSpace(form.Metodo2)
                ? 0
                : ObterValorPagamento("Valor2", form.Valor2);

            ValidarEstoqueCheckout(itensCheckout);

            var subtotal = itensCheckout.Sum(i => i.PrecoUnitario * i.Quantidade);
            var frete = CalcularFrete(form.Quantidade);
            var desconto = CalcularDesconto(form.Cupom, subtotal);
            var total = subtotal + frete - desconto;

            ValidarPagamentos(clienteId.Value, form, total);
            if (!ModelState.IsValid) {
                return View("Checkout", MontarCheckoutViewModel(clienteId.Value, form));
            }

            using var transaction = _context.Database.BeginTransaction();

            var enderecoId = ResolverEndereco(clienteId.Value, form);
            if (enderecoId == null) {
                transaction.Rollback();
                return View("Checkout", MontarCheckoutViewModel(clienteId.Value, form));
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

                foreach (var item in itensCheckout) {
                    pedido.Itens.Add(new PedidoItem {
                        LivroId = item.Livro.Id,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = item.PrecoUnitario
                    });
                }

                AdicionarPagamentoAoPedido(clienteId.Value, form.Metodo1, form.Valor1, form.CartaoId1, form.SalvarNovoCartao1,
                    form.NomeCartao1, form.NumeroCartao1, form.Validade1, form.CVV1, pedido);

                if (!string.IsNullOrWhiteSpace(form.Metodo2)) {
                    AdicionarPagamentoAoPedido(clienteId.Value, form.Metodo2, form.Valor2, form.CartaoId2, form.SalvarNovoCartao2,
                        form.NomeCartao2, form.NumeroCartao2, form.Validade2, form.CVV2, pedido);
                }

                _context.Pedidos.Add(pedido);
                _context.SaveChanges();
                transaction.Commit();

                if (form.UsarCarrinho) {
                    LimparCarrinho();
                }

                return RedirectToAction("PedidoConfirmado", new { id = pedido.Id });
            }
            catch {
                transaction.Rollback();
                ModelState.AddModelError(string.Empty, "Não foi possível finalizar o pedido. Tente novamente.");
                return View("Checkout", MontarCheckoutViewModel(clienteId.Value, form));
            }
        }

        public IActionResult PedidoConfirmado(int id) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth");
            }

            var pedido = _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .FirstOrDefault(p => p.Id == id && p.ClienteId == clienteId.Value);

            if (pedido == null) {
                return NotFound();
            }

            var primeiroItem = pedido.Itens.FirstOrDefault();

            var vm = new PedidoConfirmadoViewModel {
                PedidoId = pedido.Id,
                Status = pedido.Status,
                Total = pedido.Total,
                LivroTitulo = primeiroItem?.Livro?.Titulo ?? "Pedido realizado",
                Quantidade = pedido.Itens.Sum(i => i.Quantidade)
            };

            return View(vm);
        }

        private CarrinhoViewModel MontarCarrinhoViewModel() {
            var carrinho = ObterCarrinhoDaSessao();
            if (!carrinho.Any()) {
                return new CarrinhoViewModel();
            }

            var livroIds = carrinho.Select(i => i.LivroId).Distinct().ToList();
            var livros = _context.Livros
                .Include(l => l.Estoque)
                .Where(l => livroIds.Contains(l.Id) && l.IsAtivo)
                .ToList();

            var itens = new List<CarrinhoItemViewModel>();

            foreach (var item in carrinho) {
                var livro = livros.FirstOrDefault(l => l.Id == item.LivroId);
                if (livro == null) {
                    continue;
                }

                var estoqueDisponivel = livro.Estoque?.Quantidade ?? 0;
                var quantidadeAjustada = Math.Min(item.Quantidade, Math.Max(estoqueDisponivel, 0));

                if (quantidadeAjustada <= 0) {
                    continue;
                }

                itens.Add(new CarrinhoItemViewModel {
                    LivroId = livro.Id,
                    Titulo = livro.Titulo,
                    Autor = livro.Autor,
                    ImagemUrl = livro.ImagemUrl,
                    PrecoUnitario = livro.Preco,
                    Quantidade = quantidadeAjustada,
                    EmEstoque = estoqueDisponivel > 0,
                    EstoqueDisponivel = estoqueDisponivel
                });
            }

            var carrinhoAtualizado = itens
                .Select(i => new CarrinhoSessionItem { LivroId = i.LivroId, Quantidade = i.Quantidade })
                .ToList();

            var carrinhoMudou = carrinhoAtualizado.Count != carrinho.Count;

            if (!carrinhoMudou) {
                for (var indice = 0; indice < carrinhoAtualizado.Count; indice++) {
                    if (carrinhoAtualizado[indice].LivroId != carrinho[indice].LivroId ||
                        carrinhoAtualizado[indice].Quantidade != carrinho[indice].Quantidade) {
                        carrinhoMudou = true;
                        break;
                    }
                }
            }

            if (carrinhoMudou) {
                SalvarCarrinhoNaSessao(carrinhoAtualizado);
            }

            return new CarrinhoViewModel {
                Itens = itens
            };
        }

        private List<CheckoutItemRequest> ObterItensCheckout(CheckoutFormData form) {
            if (form.UsarCarrinho) {
                return ObterItensCarrinhoParaCheckout();
            }

            var livro = _context.Livros
                .Include(l => l.Estoque)
                .FirstOrDefault(l => l.Id == form.LivroId && l.IsAtivo);

            if (livro == null) {
                return new List<CheckoutItemRequest>();
            }

            return new List<CheckoutItemRequest> {
                new CheckoutItemRequest {
                    Livro = livro,
                    Quantidade = form.Quantidade <= 0 ? 1 : form.Quantidade,
                    PrecoUnitario = livro.Preco
                }
            };
        }

        private List<CheckoutItemRequest> ObterItensCarrinhoParaCheckout() {
            var carrinho = ObterCarrinhoDaSessao();
            if (!carrinho.Any()) {
                return new List<CheckoutItemRequest>();
            }

            var livroIds = carrinho.Select(i => i.LivroId).Distinct().ToList();
            var livros = _context.Livros
                .Include(l => l.Estoque)
                .Where(l => livroIds.Contains(l.Id) && l.IsAtivo)
                .ToList();

            var itens = new List<CheckoutItemRequest>();

            foreach (var item in carrinho) {
                var livro = livros.FirstOrDefault(l => l.Id == item.LivroId);
                if (livro == null) {
                    continue;
                }

                var estoqueDisponivel = livro.Estoque?.Quantidade ?? 0;
                var quantidade = Math.Min(item.Quantidade, Math.Max(estoqueDisponivel, 0));
                if (quantidade <= 0) {
                    continue;
                }

                itens.Add(new CheckoutItemRequest {
                    Livro = livro,
                    Quantidade = quantidade,
                    PrecoUnitario = livro.Preco
                });
            }

            return itens;
        }

        private void ValidarEstoqueCheckout(List<CheckoutItemRequest> itensCheckout) {
            foreach (var item in itensCheckout) {
                var estoqueDisponivel = item.Livro.Estoque?.Quantidade ?? 0;
                if (estoqueDisponivel < item.Quantidade) {
                    ModelState.AddModelError(string.Empty, $"O livro \"{item.Livro.Titulo}\" não possui estoque suficiente para concluir a compra.");
                }
            }
        }

        private List<CarrinhoSessionItem> ObterCarrinhoDaSessao() {
            var carrinhoJson = HttpContext.Session.GetString(CarrinhoSessionKey);
            if (string.IsNullOrWhiteSpace(carrinhoJson)) {
                return new List<CarrinhoSessionItem>();
            }

            return JsonSerializer.Deserialize<List<CarrinhoSessionItem>>(carrinhoJson) ?? new List<CarrinhoSessionItem>();
        }

        private void SalvarCarrinhoNaSessao(List<CarrinhoSessionItem> itens) {
            if (itens == null || !itens.Any()) {
                HttpContext.Session.Remove(CarrinhoSessionKey);
                return;
            }

            var carrinhoJson = JsonSerializer.Serialize(itens);
            HttpContext.Session.SetString(CarrinhoSessionKey, carrinhoJson);
        }

        private void LimparCarrinho() {
            HttpContext.Session.Remove(CarrinhoSessionKey);
        }

        private CheckoutViewModel MontarCheckoutViewModel(int clienteId, CheckoutFormData form) {
            var itensCheckout = ObterItensCheckout(form);
            var enderecos = _enderecoService.ListarPorCliente(clienteId) ?? new List<Endereco>();
            var cartoes = _context.Cartoes
                .Where(c => c.ClienteId == clienteId)
                .OrderByDescending(c => c.IsPadrao)
                .ToList();

            if (form.EnderecoId == 0 && enderecos.Any() && string.IsNullOrWhiteSpace(form.Logradouro)) {
                var enderecoPadrao = enderecos.FirstOrDefault(e => e.IsPadrao) ?? enderecos.First();
                form.EnderecoId = enderecoPadrao.Id;
            }

            var subtotal = itensCheckout.Sum(i => i.PrecoUnitario * i.Quantidade);
            var quantidadeTotal = itensCheckout.Sum(i => i.Quantidade);
            var frete = CalcularFrete(quantidadeTotal);
            var desconto = CalcularDesconto(form.Cupom, subtotal);
            var primeiroLivro = itensCheckout.FirstOrDefault()?.Livro;

            if (quantidadeTotal > 0) {
                form.Quantidade = quantidadeTotal;
                if (primeiroLivro != null) {
                    form.LivroId = primeiroLivro.Id;
                }
            }

            return new CheckoutViewModel {
                Livro = primeiroLivro,
                Itens = itensCheckout.Select(i => new CheckoutResumoItemViewModel {
                    LivroId = i.Livro.Id,
                    Titulo = i.Livro.Titulo,
                    Autor = i.Livro.Autor,
                    ImagemUrl = i.Livro.ImagemUrl,
                    PrecoUnitario = i.PrecoUnitario,
                    Quantidade = i.Quantidade
                }).ToList(),
                Enderecos = enderecos,
                Cartoes = cartoes,
                Quantidade = quantidadeTotal,
                Subtotal = subtotal,
                Frete = frete,
                Desconto = desconto,
                Total = subtotal + frete - desconto,
                OrigemCarrinho = form.UsarCarrinho,
                PermiteAlterarQuantidade = !form.UsarCarrinho && itensCheckout.Count == 1,
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

            ValidarPagamentoCartao(clienteId, form.Metodo1 ?? string.Empty, valor1, form.CartaoId1, form.NomeCartao1, form.NumeroCartao1, form.Validade1, form.CVV1, usaCupom, 1);

            if (!string.IsNullOrWhiteSpace(form.Metodo2)) {
                ValidarPagamentoCartao(clienteId, form.Metodo2, valor2, form.CartaoId2, form.NomeCartao2, form.NumeroCartao2, form.Validade2, form.CVV2, usaCupom, 2);
            }
        }

        private void ValidarPagamentoCartao(int clienteId, string metodo, decimal valor, int? cartaoId,
            string? nome, string? numero, string? validade, string? cvv, bool usaCupom, int indice) {
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

        private void AdicionarPagamentoAoPedido(int clienteId, string? metodo, decimal? valor, int? cartaoId, bool salvarNovoCartao,
            string? nomeCartao, string? numeroCartao, string? validade, string? cvv, Pedido pedido) {
            if (string.IsNullOrWhiteSpace(metodo) || !valor.HasValue || valor.Value <= 0) {
                return;
            }

            if (string.Equals(metodo, "cartao", StringComparison.OrdinalIgnoreCase) && (!cartaoId.HasValue || cartaoId.Value == 0) && salvarNovoCartao) {
                var novoCartao = new Cartao {
                    ClienteId = clienteId,
                    NomeImpresso = (nomeCartao ?? string.Empty).Trim(),
                    Numero = (numeroCartao ?? string.Empty).Trim(),
                    Validade = (validade ?? string.Empty).Trim(),
                    CVV = (cvv ?? string.Empty).Trim()
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

        private decimal CalcularDesconto(string? cupom, decimal subtotal) {
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

            var normalizado = valorBruto.Trim().Replace(".", string.Empty).Replace(',', '.');

            if (decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorNormalizado)) {
                return valorNormalizado;
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

        private sealed class CheckoutItemRequest {
            public Livro Livro { get; set; } = null!;
            public int Quantidade { get; set; }
            public decimal PrecoUnitario { get; set; }
        }
    }
}
