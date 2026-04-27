using Livros.Domain;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Services;
using Livros.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Livros.Web.Controllers {
    public class PedidoController : Controller {
        private const string CarrinhoSessionKey = "Carrinho";
        private static readonly TimeSpan ReservaCarrinhoDuracao = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ReservaCarrinhoAviso = TimeSpan.FromMinutes(5);

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
            LimparReservasExpiradas();

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
            var clienteId = ObterClienteId();
            var sessionKey = ObterSessionKeyReserva();
            var quantidadeDesejada = (itemExistente?.Quantidade ?? 0) + quantidade;
            var quantidadeDisponivel = ObterQuantidadeDisponivelParaUsuario(livroId, estoqueDisponivel, clienteId, sessionKey);
            var quantidadeFinal = Math.Min(quantidadeDesejada, quantidadeDisponivel);

            if (quantidadeFinal <= 0) {
                TempData["ErroCarrinho"] = $"O livro \"{livro.Titulo}\" nao possui saldo disponivel para reserva no momento.";
                return RedirecionarParaOrigemOuHome();
            }

            if (itemExistente == null) {
                carrinho.Add(new CarrinhoSessionItem {
                    LivroId = livroId,
                    Quantidade = quantidadeFinal
                });
            }
            else {
                itemExistente.Quantidade = quantidadeFinal;
            }

            CriarOuAtualizarReservaCarrinho(livroId, quantidadeFinal, clienteId, sessionKey, renovarExpiracao: true);
            _context.SaveChanges();

            if (quantidadeFinal < quantidadeDesejada) {
                TempData["ErroCarrinho"] = $"O estoque reservado de \"{livro.Titulo}\" foi ajustado para {quantidadeFinal} unidade(s).";
            }
            else {
                TempData["SucessoCarrinho"] = $"\"{livro.Titulo}\" foi adicionado ao carrinho.";
            }

            SalvarCarrinhoNaSessao(carrinho);
            return RedirecionarParaOrigemOuHome();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AtualizarCarrinho(int livroId, int quantidade) {
            LimparReservasExpiradas();

            var carrinho = ObterCarrinhoDaSessao();
            var item = carrinho.FirstOrDefault(i => i.LivroId == livroId);

            if (item == null) {
                return RedirectToAction(nameof(Carrinho));
            }

            if (quantidade <= 0) {
                carrinho.Remove(item);
                RemoverReservaCarrinho(livroId, ObterClienteId(), ObterSessionKeyReserva());
                _context.SaveChanges();
                SalvarCarrinhoNaSessao(carrinho);
                return RedirectToAction(nameof(Carrinho));
            }

            var livro = _context.Livros
                .Include(l => l.Estoque)
                .FirstOrDefault(l => l.Id == livroId && l.IsAtivo);

            if (livro == null) {
                carrinho.Remove(item);
                RemoverReservaCarrinho(livroId, ObterClienteId(), ObterSessionKeyReserva());
                _context.SaveChanges();
                SalvarCarrinhoNaSessao(carrinho);
                TempData["ErroCarrinho"] = "O item nao esta mais disponivel.";
                return RedirectToAction(nameof(Carrinho));
            }

            var estoqueDisponivel = livro.Estoque?.Quantidade ?? 0;
            var clienteId = ObterClienteId();
            var sessionKey = ObterSessionKeyReserva();
            var quantidadeDisponivel = ObterQuantidadeDisponivelParaUsuario(livroId, estoqueDisponivel, clienteId, sessionKey);
            var quantidadeFinal = Math.Min(Math.Max(1, quantidade), quantidadeDisponivel);

            if (quantidadeFinal <= 0) {
                carrinho.Remove(item);
                RemoverReservaCarrinho(livroId, clienteId, sessionKey);
                _context.SaveChanges();
                SalvarCarrinhoNaSessao(carrinho);
                TempData["ErroCarrinho"] = $"O livro \"{livro.Titulo}\" ficou sem saldo reservado no momento.";
                return RedirectToAction(nameof(Carrinho));
            }

            item.Quantidade = quantidadeFinal;
            CriarOuAtualizarReservaCarrinho(livroId, quantidadeFinal, clienteId, sessionKey, renovarExpiracao: true);
            _context.SaveChanges();

            SalvarCarrinhoNaSessao(carrinho);
            if (quantidadeFinal < quantidade) {
                TempData["ErroCarrinho"] = $"A quantidade de \"{livro.Titulo}\" foi ajustada para {quantidadeFinal} unidade(s) por falta de estoque reservado.";
            }
            return RedirectToAction(nameof(Carrinho));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoverDoCarrinho(int livroId) {
            var carrinho = ObterCarrinhoDaSessao();
            carrinho.RemoveAll(i => i.LivroId == livroId);
            RemoverReservaCarrinho(livroId, ObterClienteId(), ObterSessionKeyReserva());
            _context.SaveChanges();
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
                UsarCarrinho = false,
                TipoEntrega = "PADRAO",
                DataEntregaPrevista = null
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
                UsarCarrinho = true,
                TipoEntrega = "PADRAO",
                DataEntregaPrevista = null
            };

            var sincronizacao = SincronizarCarrinhoComEstoque(renovarReservas: true);
            var vm = MontarCheckoutViewModel(clienteId.Value, form, sincronizacao);
            if (!vm.Itens.Any()) {
                TempData["ErroCarrinho"] = "Seu carrinho esta vazio.";
                return RedirectToAction(nameof(Carrinho));
            }

            return View("Checkout", vm);
        }

        [HttpGet]
        public IActionResult ValidarCupom(string? codigo, decimal subtotal, decimal frete = 0, [FromQuery] List<int>? cuponsTrocaSelecionados = null) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return Json(new { valido = false, mensagem = "Faca login para aplicar um cupom." });
            }

            if (subtotal <= 0) {
                return Json(new { valido = false, mensagem = "Subtotal invalido para aplicar o cupom." });
            }

            var form = new CheckoutFormData {
                Cupom = codigo,
                CuponsTrocaSelecionados = cuponsTrocaSelecionados ?? new List<int>()
            };
            var aplicacaoCupons = CalcularAplicacaoCupons(clienteId.Value, form, subtotal, frete);
            var desconto = aplicacaoCupons.DescontoTotal;

            if (desconto <= 0) {
                return Json(new { valido = false, mensagem = "Cupom invalido ou indisponivel." });
            }

            return Json(new {
                valido = true,
                codigo = aplicacaoCupons.CodigoPromocionalAplicado ?? codigo?.Trim(),
                desconto,
                mensagem = aplicacaoCupons.Mensagem ?? "Cupom aplicado com sucesso.",
                cuponsTrocaAplicados = aplicacaoCupons.CuponsTrocaAplicados.Select(c => c.Id).ToList()
            });
        }

        [HttpGet]
        public IActionResult CalcularFreteCheckout(int? enderecoId, string? estado, int quantidade = 1) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return Json(new { sucesso = false, mensagem = "Faca login para calcular o frete." });
            }

            var estadoDestino = ResolverEstadoFrete(clienteId.Value, enderecoId, estado);
            var frete = CalcularFrete(quantidade, estadoDestino);

            return Json(new {
                sucesso = true,
                estado = estadoDestino,
                frete
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FinalizarPedido(CheckoutFormData form) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Carrinho), "Pedido") });
            }

            form.TipoEntrega = NormalizarTipoEntrega(form.TipoEntrega);
            form.DataEntregaPrevista = NormalizarDataEntregaPrevista(form.TipoEntrega, form.DataEntregaPrevista);
            form.Valor1 = ObterValorPagamento("Valor1", form.Valor1);
            form.Valor2 = ObterValorPagamento("Valor2", form.Valor2);

            CarrinhoSyncResult? sincronizacaoCarrinho = null;
            if (form.UsarCarrinho) {
                sincronizacaoCarrinho = SincronizarCarrinhoComEstoque(renovarReservas: true);
            }

            var itensCheckout = ObterItensCheckout(form, sincronizacaoCarrinho);
            if (!itensCheckout.Any()) {
                TempData["ErroCarrinho"] = "Nao ha itens validos para finalizar a compra.";
                return form.UsarCarrinho
                    ? RedirectToAction(nameof(Carrinho))
                    : RedirectToAction("Index", "Home");
            }

            if (form.UsarCarrinho && sincronizacaoCarrinho?.RequerRevisao == true) {
                ModelState.AddModelError(string.Empty, "Seu carrinho foi atualizado por alteracao de estoque ou expiracao da reserva. Revise os itens antes de finalizar.");
            }

            ValidarEstoqueCheckout(itensCheckout);

            var enderecoId = ResolverEndereco(clienteId.Value, form);
            var subtotal = itensCheckout.Sum(i => i.PrecoUnitario * i.Quantidade);
            var quantidadeTotal = itensCheckout.Sum(i => i.Quantidade);
            var estadoFrete = ObterEstadoFreteDoFormularioOuEndereco(clienteId.Value, form, enderecoId);
            var frete = CalcularFrete(quantidadeTotal, estadoFrete);
            var aplicacaoCupons = CalcularAplicacaoCupons(clienteId.Value, form, subtotal, frete);
            var desconto = aplicacaoCupons.DescontoTotal;
            var total = Math.Max(subtotal + frete - desconto, 0);

            if (string.Equals(form.TipoEntrega, "PROGRAMADA", StringComparison.OrdinalIgnoreCase)) {
                var dataMinimaEntregaProgramada = ObterDataMinimaEntregaProgramada();

                if (!form.DataEntregaPrevista.HasValue) {
                    ModelState.AddModelError(nameof(form.DataEntregaPrevista), "Informe a data prevista para a entrega programada.");
                }
                else if (form.DataEntregaPrevista.Value.Date < dataMinimaEntregaProgramada) {
                    ModelState.AddModelError(nameof(form.DataEntregaPrevista), $"A entrega programada deve ser agendada para {dataMinimaEntregaProgramada:dd/MM/yyyy} ou uma data posterior.");
                }
            }

            ValidarPagamentos(clienteId.Value, form, total, aplicacaoCupons);

            if (!ModelState.IsValid || !enderecoId.HasValue) {
                var vmInvalido = MontarCheckoutViewModel(clienteId.Value, form, sincronizacaoCarrinho);
                return View("Checkout", vmInvalido);
            }

            var pedido = new Pedido {
                ClienteId = clienteId.Value,
                EnderecoId = enderecoId.Value,
                Data = DateTime.Now,
                Total = total,
                TipoEntrega = form.TipoEntrega,
                DataEntregaPrevista = form.DataEntregaPrevista,
                Status = "APROVADA",
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

            AdicionarPagamentoAoPedido(clienteId.Value, form.Metodo1, form.Valor1, form.CartaoId1, form.BandeiraCartaoId1, form.SalvarNovoCartao1,
                form.NomeCartao1, form.NumeroCartao1, form.Validade1, form.CVV1, pedido);

            AdicionarPagamentoAoPedido(clienteId.Value, form.Metodo2, form.Valor2, form.CartaoId2, form.BandeiraCartaoId2, form.SalvarNovoCartao2,
                form.NomeCartao2, form.NumeroCartao2, form.Validade2, form.CVV2, pedido);

            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            if (aplicacaoCupons.CupomPromocional != null) {
                MarcarCupomComoUtilizado(aplicacaoCupons.CupomPromocional, pedido, aplicacaoCupons.DescontoPromocional);
            }

            if (aplicacaoCupons.CuponsTrocaAplicados.Any()) {
                MarcarCuponsTrocaComoUtilizados(aplicacaoCupons.CuponsTrocaAplicados, pedido, aplicacaoCupons.DescontoTroca);
            }

            if (aplicacaoCupons.CupomPromocional != null || aplicacaoCupons.CuponsTrocaAplicados.Any()) {
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
                Status = FormatarStatusPedido(pedido.Status, _context.Trocas.Where(t => t.PedidoId == pedido.Id).ToList()),
                TipoEntrega = FormatarTipoEntrega(pedido.TipoEntrega),
                DataEntregaPrevista = pedido.DataEntregaPrevista,
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

            var trocasPorPedido = _context.Trocas
                .Where(t => pedidos.Select(p => p.Id).Contains(t.PedidoId))
                .ToList()
                .GroupBy(t => t.PedidoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var vm = new MeusPedidosViewModel {
                Pedidos = pedidos.Select(p => {
                    var itemPrincipal = p.Itens.FirstOrDefault();
                    return new MeusPedidosItemViewModel {
                        PedidoId = p.Id,
                        Data = p.Data,
                        Total = p.Total,
                        Status = FormatarStatusPedido(p.Status, trocasPorPedido.TryGetValue(p.Id, out var trocasPedido) ? trocasPedido : null),
                        TipoEntrega = FormatarTipoEntrega(p.TipoEntrega),
                        DataEntregaPrevista = p.DataEntregaPrevista,
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

            var statusPedidoExibicao = FormatarStatusPedido(pedido.Status, trocas);

            var vm = new DetalhesPedidoViewModel {
                PedidoId = pedido.Id,
                Data = pedido.Data,
                Status = statusPedidoExibicao,
                TipoEntrega = FormatarTipoEntrega(pedido.TipoEntrega),
                DataEntregaPrevista = pedido.DataEntregaPrevista,
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
                        PedidoEntregue = statusPedidoExibicao == "ENTREGUE",
                        TrocaId = troca?.Id,
                        TrocaStatus = NormalizarStatusTrocaExibicao(troca),
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

            var statusPedidoExibicao = NormalizarStatusPedidoExibicao(pedidoItem.Pedido?.Status);
            if (statusPedidoExibicao != "ENTREGUE") {
                TempData["ErroTroca"] = "A troca so pode ser solicitada para pedidos ENTREGUE.";
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
                Status = "EM TROCA",
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
            var sincronizacao = SincronizarCarrinhoComEstoque(renovarReservas: false);

            return new CarrinhoViewModel {
                Avisos = sincronizacao.Avisos,
                Itens = sincronizacao.Itens.Select(item => new CarrinhoItemViewModel {
                    LivroId = item.Livro.Id,
                    Titulo = item.Livro.Titulo,
                    Autor = item.Livro.Autor,
                    ImagemUrl = item.Livro.ImagemUrl,
                    PrecoUnitario = item.Livro.Preco,
                    Quantidade = item.Quantidade,
                    EmEstoque = item.EstoqueDisponivel > 0,
                    EstoqueDisponivel = item.EstoqueDisponivel,
                    ReservaExpiraEm = item.ReservaExpiraEm,
                    ReservaExpirando = item.ReservaExpirando,
                    AvisoReserva = item.AvisoReserva
                }).ToList()
            };
        }

        private List<CheckoutItemRequest> ObterItensCheckout(CheckoutFormData form, CarrinhoSyncResult? sincronizacaoCarrinho = null) {
            if (form.UsarCarrinho) {
                var sincronizacao = sincronizacaoCarrinho ?? SincronizarCarrinhoComEstoque(renovarReservas: true);
                return sincronizacao.Itens.Select(item => new CheckoutItemRequest {
                    Livro = item.Livro,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.Livro.Preco
                }).ToList();
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

        private void ValidarEstoqueCheckout(List<CheckoutItemRequest> itensCheckout) {
            foreach (var item in itensCheckout) {
                var estoqueDisponivel = item.Livro.Estoque?.Quantidade ?? 0;
                if (estoqueDisponivel < item.Quantidade) {
                    ModelState.AddModelError(string.Empty, $"O livro \"{item.Livro.Titulo}\" nao possui estoque suficiente para concluir a compra.");
                }
            }
        }

        private CarrinhoSyncResult SincronizarCarrinhoComEstoque(bool renovarReservas) {
            var resultado = new CarrinhoSyncResult();
            var agora = DateTime.Now;
            var clienteId = ObterClienteId();
            var sessionKey = ObterSessionKeyReserva();
            var livrosExpirados = ObterLivroIdsExpiradosDoUsuario(agora, clienteId, sessionKey);

            LimparReservasExpiradas(agora);

            var carrinho = ObterCarrinhoDaSessao();
            if (!carrinho.Any()) {
                return resultado;
            }

            var livroIds = carrinho.Select(i => i.LivroId).Distinct().ToList();
            var livros = _context.Livros
                .Include(l => l.Estoque)
                .Where(l => livroIds.Contains(l.Id) && l.IsAtivo)
                .ToList();

            var reservasAtivas = _context.ReservasCarrinho
                .Where(r => livroIds.Contains(r.LivroId) && r.ExpiraEm > agora)
                .ToList();

            foreach (var item in carrinho) {
                if (livrosExpirados.Contains(item.LivroId)) {
                    resultado.CarrinhoMudou = true;
                    resultado.RequerRevisao = true;
                    resultado.Avisos.Add("Um item foi removido do carrinho porque a reserva expirou.");
                    continue;
                }

                var livro = livros.FirstOrDefault(l => l.Id == item.LivroId);
                if (livro == null) {
                    RemoverReservaCarrinho(item.LivroId, clienteId, sessionKey);
                    resultado.CarrinhoMudou = true;
                    resultado.RequerRevisao = true;
                    resultado.Avisos.Add("Um item foi removido do carrinho porque nao esta mais disponivel.");
                    continue;
                }

                var estoqueDisponivel = livro.Estoque?.Quantidade ?? 0;
                var quantidadeReservadaPorOutros = reservasAtivas
                    .Where(r => r.LivroId == item.LivroId && !ReservaPertenceAoUsuario(r, clienteId, sessionKey))
                    .Sum(r => r.Quantidade);

                var disponivelAoUsuario = Math.Max(estoqueDisponivel - quantidadeReservadaPorOutros, 0);
                var quantidadeAjustada = Math.Min(item.Quantidade, disponivelAoUsuario);

                if (quantidadeAjustada <= 0) {
                    RemoverReservaCarrinho(item.LivroId, clienteId, sessionKey);
                    resultado.CarrinhoMudou = true;
                    resultado.RequerRevisao = true;
                    resultado.Avisos.Add($"\"{livro.Titulo}\" foi removido do carrinho porque ficou sem estoque.");
                    continue;
                }

                if (quantidadeAjustada != item.Quantidade) {
                    resultado.CarrinhoMudou = true;
                    resultado.RequerRevisao = true;
                    resultado.Avisos.Add($"A quantidade de \"{livro.Titulo}\" foi ajustada para {quantidadeAjustada} unidade(s) por alteracao de estoque.");
                }

                var reserva = reservasAtivas
                    .FirstOrDefault(r => r.LivroId == item.LivroId && ReservaPertenceAoUsuario(r, clienteId, sessionKey));

                if (reserva == null) {
                    reserva = CriarOuAtualizarReservaCarrinho(item.LivroId, quantidadeAjustada, clienteId, sessionKey, renovarExpiracao: true);
                    reservasAtivas.Add(reserva);
                }
                else if (reserva.Quantidade != quantidadeAjustada || renovarReservas) {
                    reserva = CriarOuAtualizarReservaCarrinho(item.LivroId, quantidadeAjustada, clienteId, sessionKey, renovarExpiracao: true);
                }

                var tempoRestante = reserva.ExpiraEm - agora;
                var reservaExpirando = tempoRestante <= ReservaCarrinhoAviso;

                resultado.Itens.Add(new CarrinhoItemNormalizado {
                    Livro = livro,
                    Quantidade = quantidadeAjustada,
                    EstoqueDisponivel = disponivelAoUsuario,
                    ReservaExpiraEm = reserva.ExpiraEm,
                    ReservaExpirando = reservaExpirando,
                    AvisoReserva = reservaExpirando
                        ? $"Reserva expira em {Math.Max((int)Math.Ceiling(tempoRestante.TotalMinutes), 0)} minuto(s)."
                        : null
                });
            }

            _context.SaveChanges();

            var carrinhoAtualizado = resultado.Itens
                .Select(i => new CarrinhoSessionItem { LivroId = i.Livro.Id, Quantidade = i.Quantidade })
                .ToList();

            if (CarrinhoMudou(carrinho, carrinhoAtualizado)) {
                resultado.CarrinhoMudou = true;
                SalvarCarrinhoNaSessao(carrinhoAtualizado);
            }

            return resultado;
        }

        private List<CarrinhoSessionItem> ObterCarrinhoDaSessao() {
            var carrinhoJson = HttpContext.Session.GetString(CarrinhoSessionKey);
            if (!string.IsNullOrWhiteSpace(carrinhoJson)) {
                return JsonSerializer.Deserialize<List<CarrinhoSessionItem>>(carrinhoJson) ?? new List<CarrinhoSessionItem>();
            }

            var clienteId = ObterClienteId();
            if (!clienteId.HasValue) {
                return new List<CarrinhoSessionItem>();
            }

            var carrinhoPersistido = _context.Clientes
                .Where(c => c.Id == clienteId.Value)
                .Select(c => c.CarrinhoPersistidoJson)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(carrinhoPersistido)) {
                return new List<CarrinhoSessionItem>();
            }

            var itens = JsonSerializer.Deserialize<List<CarrinhoSessionItem>>(carrinhoPersistido) ?? new List<CarrinhoSessionItem>();
            if (itens.Any()) {
                HttpContext.Session.SetString(CarrinhoSessionKey, JsonSerializer.Serialize(itens));
            }

            return itens;
        }

        private void SalvarCarrinhoNaSessao(List<CarrinhoSessionItem> itens) {
            if (itens == null || !itens.Any()) {
                HttpContext.Session.Remove(CarrinhoSessionKey);
                PersistirCarrinhoDoCliente(new List<CarrinhoSessionItem>());
                return;
            }

            var carrinhoJson = JsonSerializer.Serialize(itens);
            HttpContext.Session.SetString(CarrinhoSessionKey, carrinhoJson);
            PersistirCarrinhoDoCliente(itens);
        }

        private void LimparCarrinho() {
            HttpContext.Session.Remove(CarrinhoSessionKey);
            PersistirCarrinhoDoCliente(new List<CarrinhoSessionItem>());
            RemoverTodasReservasDoUsuario(ObterClienteId(), ObterSessionKeyReserva());
            _context.SaveChanges();
        }

        private void PersistirCarrinhoDoCliente(List<CarrinhoSessionItem> itens) {
            var clienteId = ObterClienteId();
            if (!clienteId.HasValue) {
                return;
            }

            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == clienteId.Value);
            if (cliente == null) {
                return;
            }

            cliente.CarrinhoPersistidoJson = itens.Any()
                ? JsonSerializer.Serialize(itens)
                : null;

            _context.SaveChanges();
        }
        private CheckoutViewModel MontarCheckoutViewModel(int clienteId, CheckoutFormData form, CarrinhoSyncResult? sincronizacaoCarrinho = null) {
            var itensCheckout = ObterItensCheckout(form, sincronizacaoCarrinho);
            var enderecos = (_enderecoService.ListarPorCliente(clienteId) ?? new List<Endereco>())
                .Where(e => e.IsEntrega)
                .OrderByDescending(e => e.IsPadrao)
                .ThenBy(e => e.NomeEndereco)
                .ToList();
            var cartoes = _context.Cartoes
                .Include(c => c.BandeiraCartao)
                .Where(c => c.ClienteId == clienteId)
                .OrderByDescending(c => c.IsPadrao)
                .ToList();
            var bandeiras = _context.BandeirasCartao
                .Where(b => b.IsAtiva)
                .OrderBy(b => b.Nome)
                .ToList();
            var cuponsTrocaDisponiveis = _context.CuponsDesconto
                .Where(c => c.IsAtivo
                    && c.DataUtilizacao == null
                    && c.ClienteId == clienteId
                    && c.Tipo == "TROCA")
                .OrderByDescending(c => c.DataCriacao)
                .ToList();

            if (form.EnderecoId == 0 && enderecos.Any() && string.IsNullOrWhiteSpace(form.Logradouro)) {
                var enderecoPadrao = enderecos.FirstOrDefault(e => e.IsPadrao) ?? enderecos.First();
                form.EnderecoId = enderecoPadrao.Id;
            }

            form.TipoEntrega = NormalizarTipoEntrega(form.TipoEntrega);
            form.DataEntregaPrevista = NormalizarDataEntregaPrevista(form.TipoEntrega, form.DataEntregaPrevista);
            form.TipoLogradouro ??= "Rua";
            form.TipoResidencia ??= "Casa";
            form.Pais ??= "Brasil";

            var subtotal = itensCheckout.Sum(i => i.PrecoUnitario * i.Quantidade);
            var quantidadeTotal = itensCheckout.Sum(i => i.Quantidade);
            var estadoFrete = ObterEstadoFreteDoFormularioOuEndereco(clienteId, form, form.EnderecoId > 0 ? form.EnderecoId : null);
            var frete = CalcularFrete(quantidadeTotal, estadoFrete);
            var aplicacaoCupons = CalcularAplicacaoCupons(clienteId, form, subtotal, frete);
            form.CuponsTrocaSelecionados = aplicacaoCupons.CuponsTrocaAplicados.Select(c => c.Id).ToList();
            form.Cupom = aplicacaoCupons.CodigoPromocionalAplicado ?? form.Cupom;
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
                Bandeiras = bandeiras,
                CuponsTrocaDisponiveis = cuponsTrocaDisponiveis.Select(c => new CheckoutCupomDisponivelViewModel {
                    Id = c.Id,
                    Codigo = c.Codigo,
                    Valor = c.Valor
                }).ToList(),
                Quantidade = quantidadeTotal,
                Subtotal = subtotal,
                Frete = frete,
                Desconto = aplicacaoCupons.DescontoTotal,
                Total = Math.Max(subtotal + frete - aplicacaoCupons.DescontoTotal, 0),
                OrigemCarrinho = form.UsarCarrinho,
                PermiteAlterarQuantidade = !form.UsarCarrinho && itensCheckout.Count == 1,
                RequerRevisaoCarrinho = form.UsarCarrinho && (sincronizacaoCarrinho?.RequerRevisao ?? false),
                AvisosCarrinho = form.UsarCarrinho ? (sincronizacaoCarrinho?.Avisos ?? new List<string>()) : new List<string>(),
                Form = form
            };
        }

        private string ObterSessionKeyReserva() {
            _ = HttpContext.Session.Id;
            return HttpContext.Session.Id;
        }

        private int ObterQuantidadeDisponivelParaUsuario(int livroId, int estoqueDisponivel, int? clienteId, string sessionKey) {
            var agora = DateTime.Now;
            var quantidadeReservadaPorOutros = _context.ReservasCarrinho
                .Where(r => r.LivroId == livroId && r.ExpiraEm > agora)
                .AsEnumerable()
                .Where(r => !ReservaPertenceAoUsuario(r, clienteId, sessionKey))
                .Sum(r => r.Quantidade);

            return Math.Max(estoqueDisponivel - quantidadeReservadaPorOutros, 0);
        }

        private HashSet<int> ObterLivroIdsExpiradosDoUsuario(DateTime agora, int? clienteId, string sessionKey) {
            var expiradas = _context.ReservasCarrinho
                .Where(r => r.ExpiraEm <= agora)
                .AsEnumerable()
                .Where(r => ReservaPertenceAoUsuario(r, clienteId, sessionKey))
                .Select(r => r.LivroId)
                .ToHashSet();

            return expiradas;
        }

        private void LimparReservasExpiradas() {
            LimparReservasExpiradas(DateTime.Now);
        }

        private void LimparReservasExpiradas(DateTime agora) {
            var reservasExpiradas = _context.ReservasCarrinho
                .Where(r => r.ExpiraEm <= agora)
                .ToList();

            if (reservasExpiradas.Any()) {
                _context.ReservasCarrinho.RemoveRange(reservasExpiradas);
                _context.SaveChanges();
            }
        }

        private ReservaCarrinho CriarOuAtualizarReservaCarrinho(int livroId, int quantidade, int? clienteId, string sessionKey, bool renovarExpiracao) {
            var agora = DateTime.Now;
            var reserva = _context.ReservasCarrinho
                .AsEnumerable()
                .FirstOrDefault(r => r.LivroId == livroId && r.ExpiraEm > agora && ReservaPertenceAoUsuario(r, clienteId, sessionKey));

            if (reserva == null) {
                reserva = new ReservaCarrinho {
                    LivroId = livroId,
                    ClienteId = clienteId,
                    SessionKey = clienteId.HasValue ? null : sessionKey,
                    Quantidade = quantidade,
                    ReservadoEm = agora,
                    ExpiraEm = agora.Add(ReservaCarrinhoDuracao)
                };

                _context.ReservasCarrinho.Add(reserva);
                return reserva;
            }

            reserva.Quantidade = quantidade;
            reserva.ReservadoEm = agora;
            if (renovarExpiracao) {
                reserva.ExpiraEm = agora.Add(ReservaCarrinhoDuracao);
            }

            return reserva;
        }

        private void RemoverReservaCarrinho(int livroId, int? clienteId, string sessionKey) {
            var reservas = _context.ReservasCarrinho
                .AsEnumerable()
                .Where(r => r.LivroId == livroId && ReservaPertenceAoUsuario(r, clienteId, sessionKey))
                .ToList();

            if (reservas.Any()) {
                _context.ReservasCarrinho.RemoveRange(reservas);
            }
        }

        private void RemoverTodasReservasDoUsuario(int? clienteId, string sessionKey) {
            var reservas = _context.ReservasCarrinho
                .AsEnumerable()
                .Where(r => ReservaPertenceAoUsuario(r, clienteId, sessionKey))
                .ToList();

            if (reservas.Any()) {
                _context.ReservasCarrinho.RemoveRange(reservas);
            }
        }

        private static bool ReservaPertenceAoUsuario(ReservaCarrinho reserva, int? clienteId, string sessionKey) {
            if (clienteId.HasValue) {
                return reserva.ClienteId == clienteId.Value;
            }

            return !reserva.ClienteId.HasValue &&
                   !string.IsNullOrWhiteSpace(reserva.SessionKey) &&
                   string.Equals(reserva.SessionKey, sessionKey, StringComparison.Ordinal);
        }

        private static bool CarrinhoMudou(List<CarrinhoSessionItem> original, List<CarrinhoSessionItem> atualizado) {
            if (original.Count != atualizado.Count) {
                return true;
            }

            for (var indice = 0; indice < atualizado.Count; indice++) {
                if (original[indice].LivroId != atualizado[indice].LivroId ||
                    original[indice].Quantidade != atualizado[indice].Quantidade) {
                    return true;
                }
            }

            return false;
        }

        private int? ResolverEndereco(int clienteId, CheckoutFormData form) {
            if (form.EnderecoId > 0) {
                var enderecoExistente = _context.Enderecos
                    .FirstOrDefault(e => e.Id == form.EnderecoId && e.ClienteId == clienteId && e.IsEntrega);

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

            var cepNormalizado = NormalizarDigitos(form.CEP);
            if (cepNormalizado.Length != 8) {
                ModelState.AddModelError(string.Empty, "O CEP deve conter exatamente 8 digitos.");
                return null;
            }

            var estadoSigla = form.Estado.Trim().ToUpper();
            if (!Regex.IsMatch(estadoSigla, "^[A-Z]{2}$")) {
                ModelState.AddModelError(string.Empty, "Informe uma UF valida com 2 letras.");
                return null;
            }
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
                CEP = cepNormalizado,
                TipoLogradouro = string.IsNullOrWhiteSpace(form.TipoLogradouro) ? "Rua" : form.TipoLogradouro.Trim(),
                Logradouro = form.Logradouro.Trim(),
                Numero = form.Numero.Trim(),
                Complemento = form.Complemento?.Trim(),
                TipoResidencia = string.IsNullOrWhiteSpace(form.TipoResidencia) ? "Casa" : form.TipoResidencia.Trim(),
                Pais = string.IsNullOrWhiteSpace(form.Pais) ? "Brasil" : form.Pais.Trim(),
                BairroId = bairroEntity.Id,
                CidadeId = cidadeEntity.Id,
                ClienteId = clienteId,
                IsPadrao = false,
                IsEntrega = true,
                IsCobranca = false
            };

            _context.Enderecos.Add(endereco);
            _context.SaveChanges();
            return endereco.Id;
        }

        private void ValidarPagamentos(int clienteId, CheckoutFormData form, decimal total, AplicacaoCuponsCheckoutResult? aplicacaoCupons = null) {
            var usaCupom = aplicacaoCupons != null && aplicacaoCupons.DescontoTotal > 0;
            var totalArredondado = decimal.Round(total, 2);

            if (totalArredondado <= 0 && usaCupom) {
                return;
            }

            if (string.IsNullOrWhiteSpace(form.Metodo1)) {
                ModelState.AddModelError(string.Empty, "Selecione pelo menos uma forma de pagamento.");
                return;
            }

            var valor1 = form.Valor1 ?? 0;
            var valor2 = string.IsNullOrWhiteSpace(form.Metodo2) ? 0 : form.Valor2 ?? 0;
            var soma = decimal.Round(valor1 + valor2, 2);

            if (valor1 <= 0) {
                ModelState.AddModelError(string.Empty, "Informe um valor valido para o pagamento 1.");
            }

            if (!string.IsNullOrWhiteSpace(form.Metodo2) && valor2 <= 0) {
                ModelState.AddModelError(string.Empty, "Informe um valor valido para o pagamento 2.");
            }

            if (soma != totalArredondado) {
                ModelState.AddModelError(string.Empty, "A soma dos pagamentos deve ser igual ao total do pedido.");
            }

            ValidarPagamentoCartao(clienteId, form.Metodo1 ?? string.Empty, valor1, form.CartaoId1, form.BandeiraCartaoId1, form.NomeCartao1, form.NumeroCartao1, form.Validade1, form.CVV1, usaCupom, 1);

            if (!string.IsNullOrWhiteSpace(form.Metodo2)) {
                ValidarPagamentoCartao(clienteId, form.Metodo2, valor2, form.CartaoId2, form.BandeiraCartaoId2, form.NomeCartao2, form.NumeroCartao2, form.Validade2, form.CVV2, usaCupom, 2);
            }
        }

        private void ValidarPagamentoCartao(int clienteId, string metodo, decimal valor, int? cartaoId, int? bandeiraCartaoId,
            string? nome, string? numero, string? validade, string? cvv, bool usaCupom, int indice) {
            if (!string.Equals(metodo, "cartao", StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            if (!usaCupom && valor < 10) {
                ModelState.AddModelError(string.Empty, $"O pagamento {indice} com cartao deve ter valor minimo de R$ 10,00.");
            }

            if (cartaoId.HasValue && cartaoId.Value > 0) {
                var cartaoExistente = _context.Cartoes
                    .Include(c => c.BandeiraCartao)
                    .FirstOrDefault(c => c.Id == cartaoId.Value && c.ClienteId == clienteId);
                if (cartaoExistente == null) {
                    ModelState.AddModelError(string.Empty, $"Selecione um cartao valido no pagamento {indice}.");
                } else if (cartaoExistente.BandeiraCartao == null || !cartaoExistente.BandeiraCartao.IsAtiva) {
                    ModelState.AddModelError(string.Empty, $"O cartao selecionado no pagamento {indice} possui uma bandeira invalida.");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(numero) || string.IsNullOrWhiteSpace(validade) || string.IsNullOrWhiteSpace(cvv)) {
                ModelState.AddModelError(string.Empty, $"Preencha os dados completos do novo cartao no pagamento {indice}.");
                return;
            }

            if (!bandeiraCartaoId.HasValue || !_context.BandeirasCartao.Any(b => b.Id == bandeiraCartaoId.Value && b.IsAtiva)) {
                ModelState.AddModelError(string.Empty, $"Selecione uma bandeira valida no pagamento {indice}.");
            }

            var numeroNormalizado = NormalizarDigitos(numero);
            if (numeroNormalizado.Length != 16) {
                ModelState.AddModelError(string.Empty, $"O cartao do pagamento {indice} deve ter exatamente 16 digitos.");
            }

            var cvvNormalizado = NormalizarDigitos(cvv);
            if (cvvNormalizado.Length != 3) {
                ModelState.AddModelError(string.Empty, $"O CVV do pagamento {indice} deve ter exatamente 3 digitos.");
            }

            if (!Regex.IsMatch(validade.Trim(), "^(0[1-9]|1[0-2])\\/\\d{2}$")) {
                ModelState.AddModelError(string.Empty, $"A validade do pagamento {indice} deve estar no formato MM/AA.");
            }
        }

        private void AdicionarPagamentoAoPedido(int clienteId, string? metodo, decimal? valor, int? cartaoId, int? bandeiraCartaoId, bool salvarNovoCartao,
            string? nomeCartao, string? numeroCartao, string? validade, string? cvv, Pedido pedido) {
            if (string.IsNullOrWhiteSpace(metodo) || !valor.HasValue || valor.Value <= 0) {
                return;
            }

            if (string.Equals(metodo, "cartao", StringComparison.OrdinalIgnoreCase) && (!cartaoId.HasValue || cartaoId.Value == 0) && salvarNovoCartao) {
                var novoCartao = new Cartao {
                    ClienteId = clienteId,
                    NomeImpresso = (nomeCartao ?? string.Empty).Trim(),
                    Numero = NormalizarDigitos(numeroCartao),
                    Validade = (validade ?? string.Empty).Trim(),
                    CVV = NormalizarDigitos(cvv),
                    BandeiraCartaoId = bandeiraCartaoId ?? 0
                };

                _context.Cartoes.Add(novoCartao);
            }

            pedido.Pagamentos.Add(new Pagamento {
                Metodo = metodo.Trim().ToLower(),
                Valor = valor.Value,
                Status = "Pendente"
            });
        }
        private void MarcarCupomComoUtilizado(CupomDesconto cupomAplicado, Pedido pedido, decimal descontoAplicado) {
            var valorOriginal = cupomAplicado.Valor;
            var valorUtilizado = Math.Min(valorOriginal, descontoAplicado);
            var saldoRestante = Math.Max(valorOriginal - valorUtilizado, 0);

            cupomAplicado.Valor = valorUtilizado;
            cupomAplicado.IsAtivo = false;
            cupomAplicado.DataUtilizacao = DateTime.Now;
            cupomAplicado.PedidoId = pedido.Id;

            if (string.Equals(cupomAplicado.Tipo, "TROCA", StringComparison.OrdinalIgnoreCase) && saldoRestante > 0) {
                _context.CuponsDesconto.Add(new CupomDesconto {
                    Codigo = $"TROCA-{DateTime.Now:yyyyMMddHHmmss}",
                    Valor = decimal.Round(saldoRestante, 2),
                    Tipo = "TROCA",
                    IsAtivo = true,
                    ClienteId = cupomAplicado.ClienteId,
                    DataCriacao = DateTime.Now
                });
            }
        }

        private void MarcarCuponsTrocaComoUtilizados(List<CupomDesconto> cuponsAplicados, Pedido pedido, decimal descontoAplicado) {
            if (!cuponsAplicados.Any()) {
                return;
            }

            var restanteParaConsumir = descontoAplicado;
            var saldoRestanteTotal = 0m;

            foreach (var cupom in cuponsAplicados
                .OrderBy(c => c.Valor)
                .ThenBy(c => c.Id)) {
                var valorOriginal = cupom.Valor;
                var valorUtilizado = Math.Min(valorOriginal, Math.Max(restanteParaConsumir, 0));
                var saldoRestante = Math.Max(valorOriginal - valorUtilizado, 0);

                cupom.Valor = decimal.Round(valorUtilizado, 2);
                cupom.IsAtivo = false;
                cupom.DataUtilizacao = DateTime.Now;
                cupom.PedidoId = pedido.Id;

                restanteParaConsumir -= valorUtilizado;
                saldoRestanteTotal += saldoRestante;
            }

            if (saldoRestanteTotal > 0) {
                _context.CuponsDesconto.Add(new CupomDesconto {
                    Codigo = $"TROCA-{DateTime.Now:yyyyMMddHHmmss}",
                    Valor = decimal.Round(saldoRestanteTotal, 2),
                    Tipo = "TROCA",
                    IsAtivo = true,
                    ClienteId = cuponsAplicados.First().ClienteId,
                    DataCriacao = DateTime.Now
                });
            }
        }

        private string ObterEstadoFreteDoFormularioOuEndereco(int clienteId, CheckoutFormData form, int? enderecoId) {
            if (enderecoId.HasValue && enderecoId.Value > 0) {
                return ResolverEstadoFrete(clienteId, enderecoId, null);
            }

            if (!string.IsNullOrWhiteSpace(form.Estado)) {
                return form.Estado.Trim().ToUpperInvariant();
            }

            return "SP";
        }

        private string ResolverEstadoFrete(int clienteId, int? enderecoId, string? estadoInformado) {
            if (enderecoId.HasValue && enderecoId.Value > 0) {
                var estadoEndereco = _context.Enderecos
                    .Where(e => e.Id == enderecoId.Value && e.ClienteId == clienteId)
                    .Select(e => e.Cidade.Estado.Sigla)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(estadoEndereco)) {
                    return estadoEndereco.Trim().ToUpperInvariant();
                }
            }

            if (!string.IsNullOrWhiteSpace(estadoInformado)) {
                return estadoInformado.Trim().ToUpperInvariant();
            }

            return "SP";
        }

        private decimal CalcularFrete(int quantidade, string? estadoDestino) {
            if (quantidade <= 0) {
                quantidade = 1;
            }

            var uf = string.IsNullOrWhiteSpace(estadoDestino)
                ? "SP"
                : estadoDestino.Trim().ToUpperInvariant();

            decimal freteBase = uf switch {
                "SP" => 12m,
                "RJ" or "MG" or "ES" => 15m,
                "PR" or "SC" or "RS" => 18m,
                "DF" or "GO" or "MS" or "MT" => 20m,
                "BA" or "SE" or "AL" or "PE" or "PB" or "RN" or "CE" or "PI" or "MA" => 24m,
                "PA" or "AP" or "AM" or "RR" or "RO" or "AC" or "TO" => 29m,
                _ => 22m
            };

            var adicionalPorItem = uf == "SP" ? 1.50m : uf is "RJ" or "MG" or "ES" ? 2m : 2.50m;
            return freteBase + Math.Max(quantidade - 1, 0) * adicionalPorItem;
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

        private decimal CalcularDesconto(CupomDesconto? cupomAplicado, string? cupom, decimal subtotal, decimal frete = 0) {
            if (cupomAplicado != null) {
                var baseDesconto = string.Equals(cupomAplicado.Tipo, "TROCA", StringComparison.OrdinalIgnoreCase)
                    ? subtotal + frete
                    : subtotal;

                return Math.Min(baseDesconto, cupomAplicado.Valor);
            }

            if (string.IsNullOrWhiteSpace(cupom)) {
                return 0;
            }

            return string.Equals(cupom.Trim(), "DESCONTO10", StringComparison.OrdinalIgnoreCase)
                ? decimal.Round(subtotal * 0.10m, 2)
                : 0;
        }

        private AplicacaoCuponsCheckoutResult CalcularAplicacaoCupons(int clienteId, CheckoutFormData form, decimal subtotal, decimal frete) {
            var resultado = new AplicacaoCuponsCheckoutResult();
            var cupomDigitado = ObterCupomValido(clienteId, form.Cupom);

            if (cupomDigitado != null && !string.Equals(cupomDigitado.Tipo, "TROCA", StringComparison.OrdinalIgnoreCase)) {
                resultado.CupomPromocional = cupomDigitado;
                resultado.CodigoPromocionalAplicado = cupomDigitado.Codigo;
                resultado.DescontoPromocional = CalcularDesconto(cupomDigitado, null, subtotal, frete);
            } else if (string.Equals(form.Cupom?.Trim(), "DESCONTO10", StringComparison.OrdinalIgnoreCase)) {
                resultado.CodigoPromocionalAplicado = "DESCONTO10";
                resultado.DescontoPromocional = CalcularDesconto(null, form.Cupom, subtotal, frete);
            }

            var cuponsTrocaSelecionados = ObterCuponsTrocaSelecionadosValidos(clienteId, form.CuponsTrocaSelecionados);

            if (cupomDigitado != null
                && string.Equals(cupomDigitado.Tipo, "TROCA", StringComparison.OrdinalIgnoreCase)
                && cuponsTrocaSelecionados.All(c => c.Id != cupomDigitado.Id)) {
                cuponsTrocaSelecionados.Add(cupomDigitado);
            }

            var subtotalRestante = Math.Max(subtotal - resultado.DescontoPromocional, 0);
            var baseTroca = subtotalRestante + frete;

            if (!cuponsTrocaSelecionados.Any() || baseTroca <= 0) {
                return resultado;
            }

            var melhorCombinacao = EscolherMelhorCombinacaoTroca(cuponsTrocaSelecionados, baseTroca);
            resultado.CuponsTrocaAplicados = melhorCombinacao.CuponsAplicados;
            resultado.DescontoTroca = Math.Min(baseTroca, melhorCombinacao.TotalSelecionado);

            if (cuponsTrocaSelecionados.Count > resultado.CuponsTrocaAplicados.Count) {
                var quantidadeIgnorada = cuponsTrocaSelecionados.Count - resultado.CuponsTrocaAplicados.Count;
                resultado.Mensagem = quantidadeIgnorada == 1
                    ? "Selecionamos automaticamente apenas os cupons necessarios para esta compra."
                    : "Selecionamos automaticamente a melhor combinacao de cupons para evitar sobra desnecessaria.";
            }

            return resultado;
        }

        private List<CupomDesconto> ObterCuponsTrocaSelecionadosValidos(int clienteId, IEnumerable<int>? cupomIds) {
            var ids = (cupomIds ?? Enumerable.Empty<int>())
                .Distinct()
                .ToList();

            if (!ids.Any()) {
                return new List<CupomDesconto>();
            }

            return _context.CuponsDesconto
                .Where(c => ids.Contains(c.Id)
                    && c.ClienteId == clienteId
                    && c.IsAtivo
                    && c.DataUtilizacao == null
                    && c.Tipo == "TROCA")
                .OrderBy(c => c.Valor)
                .ToList();
        }

        private MelhorCombinacaoCuponsTrocaResult EscolherMelhorCombinacaoTroca(List<CupomDesconto> cupons, decimal valorAlvo) {
            var melhor = new MelhorCombinacaoCuponsTrocaResult {
                CuponsAplicados = cupons.ToList(),
                TotalSelecionado = cupons.Sum(c => c.Valor)
            };

            var quantidade = cupons.Count;
            if (quantidade == 0) {
                return melhor;
            }

            List<CupomDesconto>? melhorCobertura = null;
            decimal melhorTotalCobertura = decimal.MaxValue;

            var limite = 1 << quantidade;
            for (var mascara = 1; mascara < limite; mascara++) {
                var combinacao = new List<CupomDesconto>();
                decimal total = 0;

                for (var i = 0; i < quantidade; i++) {
                    if ((mascara & (1 << i)) == 0) {
                        continue;
                    }

                    combinacao.Add(cupons[i]);
                    total += cupons[i].Valor;
                }

                if (total < valorAlvo) {
                    continue;
                }

                if (melhorCobertura == null
                    || total < melhorTotalCobertura
                    || (total == melhorTotalCobertura && combinacao.Count < melhorCobertura.Count)) {
                    melhorCobertura = combinacao;
                    melhorTotalCobertura = total;
                }
            }

            if (melhorCobertura != null) {
                melhor.CuponsAplicados = melhorCobertura;
                melhor.TotalSelecionado = melhorTotalCobertura;
            }

            return melhor;
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

        private string FormatarStatusPedido(string? statusAtual, IEnumerable<Troca>? trocas = null) {
            if (trocas != null && trocas.Any(TrocaConcluidaParaCliente)) {
                return "Troca efetuada";
            }

            return NormalizarStatusPedidoExibicao(statusAtual);
        }

        private static string NormalizarStatusPedidoExibicao(string? statusAtual) {
            return (statusAtual ?? string.Empty).Trim().ToUpperInvariant() switch {
                "EM PROCESSAMENTO" => "APROVADA",
                "PAGAMENTO APROVADO" => "APROVADA",
                "PAGAMENTO RECUSADO" => "REPROVADA",
                "ENVIADO" => "EM TRANSPORTE",
                "APROVADA" => "APROVADA",
                "REPROVADA" => "REPROVADA",
                "EM SEPARACAO" => "EM SEPARACAO",
                "EM TRANSPORTE" => "EM TRANSPORTE",
                "ENTREGUE" => "ENTREGUE",
                "CANCELADO" => "CANCELADO",
                _ => statusAtual ?? "Nao informado"
            };
        }

        private static bool TrocaConcluidaParaCliente(Troca troca) {
            if (troca == null) {
                return false;
            }

            if (string.Equals(troca.Status, "TROCADO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(troca.Status, "Recebida", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            return string.Equals(troca.Status, "Aprovado", StringComparison.OrdinalIgnoreCase)
                && troca.CupomDescontoId.HasValue;
        }

        private static string? NormalizarStatusTrocaExibicao(Troca? troca) {
            if (troca == null) {
                return null;
            }

            if (string.Equals(troca.Status, "TROCADO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(troca.Status, "Recebida", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(troca.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) && troca.CupomDescontoId.HasValue)) {
                return "TROCADO";
            }

            if (string.Equals(troca.Status, "TROCA AUTORIZADA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(troca.Status, "Autorizada", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(troca.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) && !troca.CupomDescontoId.HasValue)) {
                return "TROCA AUTORIZADA";
            }

            if (string.Equals(troca.Status, "TROCA RECUSADA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(troca.Status, "Recusado", StringComparison.OrdinalIgnoreCase)) {
                return "TROCA RECUSADA";
            }

            if (string.Equals(troca.Status, "EM TROCA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(troca.Status, "Solicitado", StringComparison.OrdinalIgnoreCase)) {
                return "EM TROCA";
            }

            return troca.Status;
        }

        private static string NormalizarTipoEntrega(string? tipoEntrega) {
            if (string.Equals(tipoEntrega, "PROGRAMADA", StringComparison.OrdinalIgnoreCase)) {
                return "PROGRAMADA";
            }

            return "PADRAO";
        }

        private static string FormatarTipoEntrega(string? tipoEntrega) {
            return string.Equals(tipoEntrega, "PROGRAMADA", StringComparison.OrdinalIgnoreCase)
                ? "Entrega programada"
                : "Entrega padrão";
        }

        private static DateTime? NormalizarDataEntregaPrevista(string? tipoEntrega, DateTime? dataEntregaPrevista) {
            if (!string.Equals(tipoEntrega, "PROGRAMADA", StringComparison.OrdinalIgnoreCase)) {
                return null;
            }

            return dataEntregaPrevista?.Date;
        }

        private static DateTime ObterDataMinimaEntregaProgramada() {
            return DateTime.Today.AddDays(7);
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

        private string NormalizarDigitos(string? valor) {
            if (string.IsNullOrWhiteSpace(valor)) {
                return string.Empty;
            }

            return new string(valor.Where(char.IsDigit).ToArray());
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

        private sealed class CarrinhoItemNormalizado {
            public Livro Livro { get; set; } = null!;
            public int Quantidade { get; set; }
            public int EstoqueDisponivel { get; set; }
            public DateTime? ReservaExpiraEm { get; set; }
            public bool ReservaExpirando { get; set; }
            public string? AvisoReserva { get; set; }
        }

        private sealed class CarrinhoSyncResult {
            public List<CarrinhoItemNormalizado> Itens { get; set; } = new();
            public List<string> Avisos { get; set; } = new();
            public bool CarrinhoMudou { get; set; }
            public bool RequerRevisao { get; set; }
        }

        private sealed class AplicacaoCuponsCheckoutResult {
            public CupomDesconto? CupomPromocional { get; set; }
            public string? CodigoPromocionalAplicado { get; set; }
            public List<CupomDesconto> CuponsTrocaAplicados { get; set; } = new();
            public decimal DescontoPromocional { get; set; }
            public decimal DescontoTroca { get; set; }
            public decimal DescontoTotal => decimal.Round(DescontoPromocional + DescontoTroca, 2);
            public string? Mensagem { get; set; }
        }

        private sealed class MelhorCombinacaoCuponsTrocaResult {
            public List<CupomDesconto> CuponsAplicados { get; set; } = new();
            public decimal TotalSelecionado { get; set; }
        }

        private sealed class CarrinhoSessionItem {
            public int LivroId { get; set; }
            public int Quantidade { get; set; }
        }
    }
}

