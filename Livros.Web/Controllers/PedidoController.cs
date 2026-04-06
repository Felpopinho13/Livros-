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
            var livro = _context.Livros
                .Include(l => l.Estoque)
                .FirstOrDefault(l => l.Id == livroId && l.IsAtivo);

            if (livro == null) {
                TempData["ErroCarrinho"] = "Nao foi possivel adicionar o livro ao carrinho.";
                return RedirecionarParaOrigemOuHome();
            }

            var estoqueDisponivel = livro.Estoque?.Quantidade ?? 0;
            if (estoqueDisponivel <= 0) {
                TempData["ErroCarrinho"] = $"O livro \"{livro.Titulo}\" esta sem estoque no momento.";
                return RedirecionarParaOrigemOuHome();
            }

            quantidade = Math.Max(1, quantidade);

            var carrinho = ObterCarrinhoDaSessao();
            var itemExistente = carrinho.FirstOrDefault(i => i.LivroId == livroId);

            if (itemExistente == null) {
                carrinho.Add(new CarrinhoSessionItem {
                    LivroId = livroId,
                    Quantidade = Math.Min(quantidade, estoqueDisponivel)
                });
            }
            else {
                itemExistente.Quantidade = Math.Min(itemExistente.Quantidade + quantidade, estoqueDisponivel);
            }

            SalvarCarrinhoNaSessao(carrinho);
            TempData["SucessoCarrinho"] = $"\"{livro.Titulo}\" foi adicionado ao carrinho.";

            return RedirecionarParaOrigemOuHome();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AtualizarCarrinho(int livroId, int quantidade) {
            var carrinho = ObterCarrinhoDaSessao();
            var item = carrinho.FirstOrDefault(i => i.LivroId == livroId);

            if (item == null) {
                return RedirectToAction(nameof(Carrinho));
            }

            if (quantidade <= 0) {
                carrinho.Remove(item);
                SalvarCarrinhoNaSessao(carrinho);
                return RedirectToAction(nameof(Carrinho));
            }

            var livro = _context.Livros
                .Include(l => l.Estoque)
                .FirstOrDefault(l => l.Id == livroId && l.IsAtivo);

            if (livro == null) {
                carrinho.Remove(item);
                SalvarCarrinhoNaSessao(carrinho);
                TempData["ErroCarrinho"] = "O item nao esta mais disponivel.";
                return RedirectToAction(nameof(Carrinho));
            }

            var estoqueDisponivel = livro.Estoque?.Quantidade ?? 0;
            item.Quantidade = Math.Min(Math.Max(1, quantidade), Math.Max(estoqueDisponivel, 1));

            SalvarCarrinhoNaSessao(carrinho);
            return RedirectToAction(nameof(Carrinho));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoverDoCarrinho(int livroId) {
            var carrinho = ObterCarrinhoDaSessao();
            carrinho.RemoveAll(i => i.LivroId == livroId);
            SalvarCarrinhoNaSessao(carrinho);
            return RedirectToAction(nameof(Carrinho));
        }

        [HttpGet]
        public IActionResult Checkout(int id, int quantidade = 1) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new {
                    returnUrl = Url.Action(nameof(Checkout), "Pedido", new { id, quantidade })
                });
            }

            var form = new CheckoutFormData {
                LivroId = id,
                Quantidade = Math.Max(1, quantidade),
                UsarCarrinho = false
            };

            var vm = MontarCheckoutViewModel(clienteId.Value, form);
            if (!vm.Itens.Any()) {
                TempData["ErroCarrinho"] = "Nao foi possivel iniciar o checkout deste livro.";
                return RedirectToAction("Index", "Home");
            }

            return View(vm);
        }

        [HttpGet]
        public IActionResult CheckoutCarrinho() {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new {
                    returnUrl = Url.Action(nameof(CheckoutCarrinho), "Pedido")
                });
            }

            var form = new CheckoutFormData {
                UsarCarrinho = true
            };

            var vm = MontarCheckoutViewModel(clienteId.Value, form);
            if (!vm.Itens.Any()) {
                TempData["ErroCarrinho"] = "Seu carrinho esta vazio.";
                return RedirectToAction(nameof(Carrinho));
            }

            return View("Checkout", vm);
        }

        [HttpGet]
        public IActionResult ValidarCupom(string? codigo, decimal subtotal) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return Json(new { valido = false, mensagem = "Faca login para aplicar um cupom." });
            }

            if (subtotal <= 0) {
                return Json(new { valido = false, mensagem = "Subtotal invalido para aplicar o cupom." });
            }

            var cupomAplicado = ObterCupomValido(clienteId.Value, codigo);
            var desconto = CalcularDesconto(cupomAplicado, codigo, subtotal);

            if (desconto <= 0) {
                return Json(new { valido = false, mensagem = "Cupom invalido ou indisponivel." });
            }

            return Json(new {
                valido = true,
                codigo = cupomAplicado?.Codigo ?? codigo?.Trim(),
                desconto,
                mensagem = "Cupom aplicado com sucesso."
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FinalizarPedido(CheckoutFormData form) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Carrinho), "Pedido") });
            }

            form.Valor1 = ObterValorPagamento("Valor1", form.Valor1);
            form.Valor2 = ObterValorPagamento("Valor2", form.Valor2);

            var itensCheckout = ObterItensCheckout(form);
            if (!itensCheckout.Any()) {
                TempData["ErroCarrinho"] = "Nao ha itens validos para finalizar a compra.";
                return form.UsarCarrinho
                    ? RedirectToAction(nameof(Carrinho))
                    : RedirectToAction("Index", "Home");
            }

            ValidarEstoqueCheckout(itensCheckout);

            var enderecoId = ResolverEndereco(clienteId.Value, form);
            var subtotal = itensCheckout.Sum(i => i.PrecoUnitario * i.Quantidade);
            var quantidadeTotal = itensCheckout.Sum(i => i.Quantidade);
            var frete = CalcularFrete(quantidadeTotal);
            var cupomAplicado = ObterCupomValido(clienteId.Value, form.Cupom);
            var desconto = CalcularDesconto(cupomAplicado, form.Cupom, subtotal);
            var total = subtotal + frete - desconto;

            ValidarPagamentos(clienteId.Value, form, total, cupomAplicado);

            if (!ModelState.IsValid || !enderecoId.HasValue) {
                var vmInvalido = MontarCheckoutViewModel(clienteId.Value, form);
                return View("Checkout", vmInvalido);
            }

            var pedido = new Pedido {
                ClienteId = clienteId.Value,
                EnderecoId = enderecoId.Value,
                Data = DateTime.Now,
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

            AdicionarPagamentoAoPedido(clienteId.Value, form.Metodo2, form.Valor2, form.CartaoId2, form.SalvarNovoCartao2,
                form.NomeCartao2, form.NumeroCartao2, form.Validade2, form.CVV2, pedido);

            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            if (cupomAplicado != null) {
                MarcarCupomComoUtilizado(cupomAplicado, pedido);
                _context.SaveChanges();
            }

            if (form.UsarCarrinho) {
                LimparCarrinho();
            }

            return RedirectToAction(nameof(PedidoConfirmado), new { id = pedido.Id });
        }

        [HttpGet]
        public IActionResult PedidoConfirmado(int id) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(PedidoConfirmado), "Pedido", new { id }) });
            }

            var pedido = _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .FirstOrDefault(p => p.Id == id && p.ClienteId == clienteId.Value);

            if (pedido == null) {
                return RedirectToAction(nameof(MeusPedidos));
            }

            var itemPrincipal = pedido.Itens.FirstOrDefault();
            var vm = new PedidoConfirmadoViewModel {
                PedidoId = pedido.Id,
                Status = pedido.Status,
                Total = pedido.Total,
                LivroTitulo = itemPrincipal?.Livro?.Titulo ?? "Pedido",
                Quantidade = pedido.Itens.Sum(i => i.Quantidade)
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult MeusPedidos() {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(MeusPedidos), "Pedido") });
            }

            var pedidos = _context.Pedidos
                .Where(p => p.ClienteId == clienteId.Value)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .OrderByDescending(p => p.Data)
                .ToList();

            var vm = new MeusPedidosViewModel {
                Pedidos = pedidos.Select(p => {
                    var itemPrincipal = p.Itens.FirstOrDefault();
                    return new MeusPedidosItemViewModel {
                        PedidoId = p.Id,
                        Data = p.Data,
                        Total = p.Total,
                        Status = p.Status,
                        LivroTitulo = itemPrincipal?.Livro?.Titulo ?? "Pedido sem itens",
                        LivroAutor = itemPrincipal?.Livro?.Autor ?? string.Empty,
                        LivroImagemUrl = itemPrincipal?.Livro?.ImagemUrl ?? string.Empty,
                        QuantidadeItens = p.Itens.Count,
                        QuantidadeLivros = p.Itens.Sum(i => i.Quantidade),
                        LivroIdPrincipal = itemPrincipal?.LivroId ?? 0
                    };
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult DetalhesPedido(int id) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(DetalhesPedido), "Pedido", new { id }) });
            }

            var pedido = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Endereco)
                    .ThenInclude(e => e.Bairro)
                .Include(p => p.Endereco)
                    .ThenInclude(e => e.Cidade)
                        .ThenInclude(c => c.Estado)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .Include(p => p.Pagamentos)
                .FirstOrDefault(p => p.Id == id && p.ClienteId == clienteId.Value);

            if (pedido == null) {
                return RedirectToAction(nameof(MeusPedidos));
            }

            var trocas = _context.Trocas
                .Include(t => t.CupomDesconto)
                .Where(t => t.PedidoId == pedido.Id)
                .ToList();

            var subtotal = pedido.Itens.Sum(i => i.PrecoUnitario * i.Quantidade);
            var cuponsPedido = _context.CuponsDesconto
                .Where(c => c.PedidoId == pedido.Id)
                .ToList();
            var desconto = cuponsPedido.Sum(c => c.Valor);
            var frete = Math.Max(pedido.Total - subtotal + desconto, 0);

            var vm = new DetalhesPedidoViewModel {
                PedidoId = pedido.Id,
                Data = pedido.Data,
                Status = pedido.Status,
                ClienteNome = pedido.Cliente?.Nome ?? string.Empty,
                EnderecoNome = pedido.Endereco?.NomeEndereco ?? string.Empty,
                Logradouro = pedido.Endereco?.Logradouro ?? string.Empty,
                Numero = pedido.Endereco?.Numero ?? string.Empty,
                Complemento = pedido.Endereco?.Complemento ?? string.Empty,
                Bairro = pedido.Endereco?.Bairro?.Nome ?? string.Empty,
                Cidade = pedido.Endereco?.Cidade?.Nome ?? string.Empty,
                Estado = pedido.Endereco?.Cidade?.Estado?.Sigla ?? string.Empty,
                CEP = pedido.Endereco?.CEP ?? string.Empty,
                Subtotal = subtotal,
                Frete = frete,
                Desconto = desconto,
                Total = pedido.Total,
                Itens = pedido.Itens.Select(item => {
                    var troca = trocas.FirstOrDefault(t => t.PedidoItemId == item.Id);
                    return new DetalhesPedidoItemViewModel {
                        PedidoItemId = item.Id,
                        LivroId = item.LivroId,
                        Titulo = item.Livro?.Titulo ?? string.Empty,
                        Autor = item.Livro?.Autor ?? string.Empty,
                        ImagemUrl = item.Livro?.ImagemUrl ?? string.Empty,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = item.PrecoUnitario,
                        TrocaId = troca?.Id,
                        TrocaStatus = troca?.Status,
                        CodigoCupomTroca = troca?.CupomDesconto?.Codigo,
                        ValorCupomTroca = troca?.CupomDesconto?.Valor
                    };
                }).ToList(),
                Pagamentos = pedido.Pagamentos.Select(p => new DetalhesPedidoPagamentoViewModel {
                    Metodo = FormatarMetodoPagamento(p.Metodo),
                    Valor = p.Valor,
                    Status = p.Status
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SolicitarTroca(int pedidoId, int pedidoItemId, string motivo, string? observacaoCliente) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new {
                    returnUrl = Url.Action(nameof(DetalhesPedido), "Pedido", new { id = pedidoId })
                });
            }

            var pedidoItem = _context.PedidoItens
                .Include(i => i.Pedido)
                .Include(i => i.Livro)
                .FirstOrDefault(i => i.Id == pedidoItemId && i.PedidoId == pedidoId && i.Pedido.ClienteId == clienteId.Value);

            if (pedidoItem == null) {
                TempData["ErroTroca"] = "Nao foi possivel localizar o item para solicitar a troca.";
                return RedirectToAction(nameof(DetalhesPedido), new { id = pedidoId });
            }

            var trocaExistente = _context.Trocas.FirstOrDefault(t => t.PedidoItemId == pedidoItemId);
            if (trocaExistente != null) {
                TempData["ErroTroca"] = "Ja existe uma solicitacao de troca para este item.";
                return RedirectToAction(nameof(DetalhesPedido), new { id = pedidoId });
            }

            if (string.IsNullOrWhiteSpace(motivo)) {
                TempData["ErroTroca"] = "Selecione ou informe um motivo para solicitar a troca.";
                return RedirectToAction(nameof(DetalhesPedido), new { id = pedidoId });
            }

            var troca = new Troca {
                Codigo = GerarCodigoTroca(),
                PedidoId = pedidoId,
                PedidoItemId = pedidoItemId,
                ClienteId = clienteId.Value,
                Motivo = motivo.Trim(),
                ObservacaoCliente = observacaoCliente?.Trim(),
                Status = "Solicitado",
                DataSolicitacao = DateTime.Now
            };

            _context.Trocas.Add(troca);
            _context.SaveChanges();

            TempData["SucessoTroca"] = $"Solicitacao de troca do livro \"{pedidoItem.Livro?.Titulo}\" enviada com sucesso.";
            return RedirectToAction(nameof(DetalhesPedido), new { id = pedidoId });
        }
        private IActionResult RedirecionarParaOrigemOuHome() {
            var origem = Request.Headers.Referer.ToString();
            if (!string.IsNullOrWhiteSpace(origem)) {
                return Redirect(origem);
            }

            return RedirectToAction("Index", "Home");
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
                    ModelState.AddModelError(string.Empty, $"O livro \"{item.Livro.Titulo}\" nao possui estoque suficiente para concluir a compra.");
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
            var cupomAplicado = ObterCupomValido(clienteId, form.Cupom);
            var desconto = CalcularDesconto(cupomAplicado, form.Cupom, subtotal);
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
                    ModelState.AddModelError(string.Empty, "Selecione um endereco de entrega valido.");
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
                ModelState.AddModelError(string.Empty, "Preencha todos os campos obrigatorios do novo endereco.");
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
                NomeEndereco = string.IsNullOrWhiteSpace(form.NomeEndereco) ? "Novo Endereco" : form.NomeEndereco.Trim(),
                CEP = form.CEP.Trim(),
                Logradouro = form.Logradouro.Trim(),
                Numero = form.Numero.Trim(),
                Complemento = form.Complemento?.Trim(),
                BairroId = bairroEntity.Id,
                CidadeId = cidadeEntity.Id,
                ClienteId = clienteId,
                IsPadrao = false
            };

            _context.Enderecos.Add(endereco);
            _context.SaveChanges();
            return endereco.Id;
        }

        private void ValidarPagamentos(int clienteId, CheckoutFormData form, decimal total, CupomDesconto? cupomAplicado = null) {
            if (string.IsNullOrWhiteSpace(form.Metodo1)) {
                ModelState.AddModelError(string.Empty, "Selecione pelo menos uma forma de pagamento.");
                return;
            }

            var valor1 = form.Valor1 ?? 0;
            var valor2 = string.IsNullOrWhiteSpace(form.Metodo2) ? 0 : form.Valor2 ?? 0;
            var soma = decimal.Round(valor1 + valor2, 2);
            var totalArredondado = decimal.Round(total, 2);

            if (valor1 <= 0) {
                ModelState.AddModelError(string.Empty, "Informe um valor valido para o pagamento 1.");
            }

            if (!string.IsNullOrWhiteSpace(form.Metodo2) && valor2 <= 0) {
                ModelState.AddModelError(string.Empty, "Informe um valor valido para o pagamento 2.");
            }

            if (Math.Abs(soma - totalArredondado) > 0.01m) {
                ModelState.AddModelError(string.Empty, "A soma dos pagamentos deve ser igual ao total do pedido.");
            }

            var usaCupom = cupomAplicado != null || string.Equals(form.Cupom?.Trim(), "DESCONTO10", StringComparison.OrdinalIgnoreCase);

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
                ModelState.AddModelError(string.Empty, $"O pagamento {indice} com cartao deve ter valor minimo de R$ 10,00.");
            }

            if (cartaoId.HasValue && cartaoId.Value > 0) {
                var cartaoExistente = _context.Cartoes.FirstOrDefault(c => c.Id == cartaoId.Value && c.ClienteId == clienteId);
                if (cartaoExistente == null) {
                    ModelState.AddModelError(string.Empty, $"Selecione um cartao valido no pagamento {indice}.");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(numero) || string.IsNullOrWhiteSpace(validade) || string.IsNullOrWhiteSpace(cvv)) {
                ModelState.AddModelError(string.Empty, $"Preencha os dados completos do novo cartao no pagamento {indice}.");
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
        private void MarcarCupomComoUtilizado(CupomDesconto cupomAplicado, Pedido pedido) {
            cupomAplicado.IsAtivo = false;
            cupomAplicado.DataUtilizacao = DateTime.Now;
            cupomAplicado.PedidoId = pedido.Id;
        }

        private decimal CalcularFrete(int quantidade) {
            if (quantidade <= 0) {
                quantidade = 1;
            }

            return 15 + Math.Max(quantidade - 1, 0) * 2;
        }

        private CupomDesconto? ObterCupomValido(int clienteId, string? cupom) {
            if (string.IsNullOrWhiteSpace(cupom)) {
                return null;
            }

            return _context.CuponsDesconto
                .FirstOrDefault(c =>
                    c.IsAtivo &&
                    c.DataUtilizacao == null &&
                    c.Codigo.ToUpper() == cupom.Trim().ToUpper() &&
                    (!c.ClienteId.HasValue || c.ClienteId.Value == clienteId));
        }

        private decimal CalcularDesconto(CupomDesconto? cupomAplicado, string? cupom, decimal subtotal) {
            if (cupomAplicado != null) {
                return Math.Min(subtotal, cupomAplicado.Valor);
            }

            if (string.IsNullOrWhiteSpace(cupom)) {
                return 0;
            }

            return string.Equals(cupom.Trim(), "DESCONTO10", StringComparison.OrdinalIgnoreCase)
                ? decimal.Round(subtotal * 0.10m, 2)
                : 0;
        }

        private string GerarCodigoTroca() {
            return $"SOL-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private string FormatarMetodoPagamento(string? metodo) {
            if (string.IsNullOrWhiteSpace(metodo)) {
                return "Nao informado";
            }

            return metodo.Trim().ToLower() switch {
                "cartao" => "Cartao",
                "pix" => "Pix",
                "boleto" => "Boleto",
                _ => metodo
            };
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

        private sealed class CarrinhoSessionItem {
            public int LivroId { get; set; }
            public int Quantidade { get; set; }
        }
    }
}
