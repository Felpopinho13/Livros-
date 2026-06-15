using Livros.Domain;
using Livros.Application.AdminOrders;
using Livros.Application.Checkout;
using Livros.Application.CustomerCart;
using Livros.Application.CustomerCheckout;
using Livros.Application.CustomerOrders;
using Livros.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace Livros.Web.Controllers {
    public class PedidoController : Controller {
        private const string CarrinhoSessionKey = "Carrinho";

        private readonly CheckoutPricingService _checkoutPricingService;
        private readonly CustomerCartService _customerCartService;
        private readonly CustomerCheckoutService _customerCheckoutService;
        private readonly CustomerOrderPlacementService _customerOrderPlacementService;
        private readonly CustomerOrdersService _customerOrdersService;

        public PedidoController(CheckoutPricingService checkoutPricingService, CustomerCartService customerCartService, CustomerCheckoutService customerCheckoutService, CustomerOrderPlacementService customerOrderPlacementService, CustomerOrdersService customerOrdersService) {
            _checkoutPricingService = checkoutPricingService;
            _customerCartService = customerCartService;
            _customerCheckoutService = customerCheckoutService;
            _customerOrderPlacementService = customerOrderPlacementService;
            _customerOrdersService = customerOrdersService;
        }

        [HttpGet]
        public IActionResult Carrinho() {
            var vm = MontarCarrinhoViewModel();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdicionarAoCarrinho(int livroId, int quantidade = 1) {
            var result = _customerCartService.AddItem(new CustomerCartAddCommand {
                Items = ObterCarrinhoDaSessao(),
                LivroId = livroId,
                Quantidade = quantidade,
                CustomerId = ObterClienteId(),
                SessionKey = ObterSessionKeyReserva()
            });

            if (!result.Success) {
                TempData["ErroCarrinho"] = result.ErrorMessage;
                return RedirecionarParaOrigemOuHome();
            }

            if (!string.IsNullOrWhiteSpace(result.WarningMessage)) {
                TempData["ErroCarrinho"] = result.WarningMessage;
            }
            else if (!string.IsNullOrWhiteSpace(result.SuccessMessage)) {
                TempData["SucessoCarrinho"] = result.SuccessMessage;
            }

            SalvarCarrinhoNaSessao(result.Items);
            return RedirecionarParaOrigemOuHome();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AtualizarCarrinho(int livroId, int quantidade) {
            var result = _customerCartService.UpdateItem(new CustomerCartUpdateCommand {
                Items = ObterCarrinhoDaSessao(),
                LivroId = livroId,
                Quantidade = quantidade,
                CustomerId = ObterClienteId(),
                SessionKey = ObterSessionKeyReserva()
            });

            if (!result.ItemFound) {
                return RedirectToAction(nameof(Carrinho));
            }

            SalvarCarrinhoNaSessao(result.Items);

            if (!result.Success && !string.IsNullOrWhiteSpace(result.ErrorMessage)) {
                TempData["ErroCarrinho"] = result.ErrorMessage;
                return RedirectToAction(nameof(Carrinho));
            }

            if (!string.IsNullOrWhiteSpace(result.WarningMessage)) {
                TempData["ErroCarrinho"] = result.WarningMessage;
            }

            return RedirectToAction(nameof(Carrinho));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoverDoCarrinho(int livroId) {
            var result = _customerCartService.RemoveItem(new CustomerCartRemoveCommand {
                Items = ObterCarrinhoDaSessao(),
                LivroId = livroId,
                CustomerId = ObterClienteId(),
                SessionKey = ObterSessionKeyReserva()
            });

            SalvarCarrinhoNaSessao(result.Items);
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

            var aplicacaoCupons = _checkoutPricingService.ApplyCoupons(new CheckoutCouponApplicationRequest {
                ClienteId = clienteId.Value,
                CodigoCupom = codigo,
                CuponsTrocaSelecionados = cuponsTrocaSelecionados ?? new List<int>(),
                Subtotal = subtotal,
                Frete = frete
            });
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

            var freteResult = _checkoutPricingService.CalculateShipping(new CheckoutShippingRequest {
                ClienteId = clienteId.Value,
                EnderecoId = enderecoId,
                EstadoInformado = estado,
                Quantidade = quantidade
            });

            return Json(new {
                sucesso = true,
                estado = freteResult.EstadoDestino,
                frete = freteResult.Frete
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FinalizarPedido(CheckoutFormData form) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Carrinho), "Pedido") });
            }

            form.TipoEntrega = CustomerCheckoutService.NormalizeDeliveryType(form.TipoEntrega);
            form.DataEntregaPrevista = CustomerCheckoutService.NormalizeScheduledDate(form.TipoEntrega, form.DataEntregaPrevista);
            form.Valor1 = ObterValorPagamento("Valor1", form.Valor1);
            form.Valor2 = ObterValorPagamento("Valor2", form.Valor2);
            form.Valor3 = ObterValorPagamento("Valor3", form.Valor3);
            form.Valor4 = ObterValorPagamento("Valor4", form.Valor4);

            CustomerCartSyncResult? sincronizacaoCarrinho = null;
            if (form.UsarCarrinho) {
                sincronizacaoCarrinho = SincronizarCarrinhoComEstoque(renovarReservas: true);
            }

            var itensCheckout = _customerCheckoutService.ResolveItems(CriarCheckoutPreparationRequest(clienteId.Value, form, sincronizacaoCarrinho));
            if (!itensCheckout.Any()) {
                TempData["ErroCarrinho"] = "Nao ha itens validos para finalizar a compra.";
                return form.UsarCarrinho
                    ? RedirectToAction(nameof(Carrinho))
                    : RedirectToAction("Index", "Home");
            }

            if (form.UsarCarrinho && sincronizacaoCarrinho?.RequerRevisao == true) {
                ModelState.AddModelError(string.Empty, "Seu carrinho foi atualizado por alteracao de estoque ou expiracao da reserva. Revise os itens antes de finalizar.");
            }

            var pagamentosCheckout = ObterPagamentosCheckout(form);
            var placement = _customerOrderPlacementService.PlaceOrder(new CustomerOrderPlacementRequest {
                CustomerId = clienteId.Value,
                UseCart = form.UsarCarrinho,
                SessionKey = ObterSessionKeyReserva(),
                DeliveryType = form.TipoEntrega,
                ScheduledDeliveryDate = form.DataEntregaPrevista,
                EnderecoId = form.EnderecoId,
                SaveNewAddress = form.SalvarNovoEndereco,
                NomeEndereco = form.NomeEndereco,
                CEP = form.CEP,
                TipoLogradouro = form.TipoLogradouro,
                Logradouro = form.Logradouro,
                Numero = form.Numero,
                Complemento = form.Complemento,
                TipoResidencia = form.TipoResidencia,
                Pais = form.Pais,
                Bairro = form.Bairro,
                Cidade = form.Cidade,
                Estado = form.Estado,
                CouponCode = form.Cupom,
                ExchangeCouponIds = form.CuponsTrocaSelecionados,
                Items = itensCheckout,
                Payments = pagamentosCheckout
            });

            form.EnderecoId = placement.ResolvedAddressId ?? form.EnderecoId;
            form.Cupom = placement.AppliedCouponCode ?? form.Cupom;
            form.CuponsTrocaSelecionados = placement.AppliedExchangeCouponIds;

            foreach (var error in placement.Errors) {
                ModelState.AddModelError(error.Key ?? string.Empty, error.Message);
            }

            if (!ModelState.IsValid || !placement.Success) {
                var invalidVm = MontarCheckoutViewModel(clienteId.Value, form, sincronizacaoCarrinho);
                return View("Checkout", invalidVm);
            }

            if (form.UsarCarrinho) {
                HttpContext.Session.Remove(CarrinhoSessionKey);
            }

            return RedirectToAction(nameof(PedidoConfirmado), new { id = placement.OrderId!.Value });
        }

        [HttpGet]
        public IActionResult PedidoConfirmado(int id) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(PedidoConfirmado), "Pedido", new { id }) });
            }

            var result = _customerOrdersService.GetConfirmation(new CustomerOrderConfirmationQuery {
                CustomerId = clienteId.Value,
                OrderId = id
            });

            if (!result.OrderFound) {
                return RedirectToAction(nameof(MeusPedidos));
            }

            var vm = new PedidoConfirmadoViewModel {
                PedidoId = result.PedidoId,
                Status = result.Status,
                TipoEntrega = result.TipoEntrega,
                DataEntregaPrevista = result.DataEntregaPrevista,
                Total = result.Total,
                LivroTitulo = result.LivroTitulo,
                Quantidade = result.Quantidade
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult MeusPedidos() {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(MeusPedidos), "Pedido") });
            }

            var result = _customerOrdersService.List(new CustomerOrdersQuery {
                CustomerId = clienteId.Value
            });

            var vm = new MeusPedidosViewModel {
                Pedidos = result.Orders.Select(order => new MeusPedidosItemViewModel {
                    PedidoId = order.PedidoId,
                    Data = order.Data,
                    Total = order.Total,
                    Status = order.Status,
                    TipoEntrega = order.TipoEntrega,
                    DataEntregaPrevista = order.DataEntregaPrevista,
                    LivroTitulo = order.LivroTitulo,
                    LivroAutor = order.LivroAutor,
                    LivroImagemUrl = order.LivroImagemUrl,
                    QuantidadeItens = order.QuantidadeItens,
                    QuantidadeLivros = order.QuantidadeLivros,
                    LivroIdPrincipal = order.LivroIdPrincipal
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

            var result = _customerOrdersService.GetDetails(new CustomerOrderDetailsQuery {
                CustomerId = clienteId.Value,
                OrderId = id
            });

            if (!result.OrderFound) {
                return RedirectToAction(nameof(MeusPedidos));
            }

            var vm = new DetalhesPedidoViewModel {
                PedidoId = result.PedidoId,
                Data = result.Data,
                Status = result.Status,
                TipoEntrega = result.TipoEntrega,
                DataEntregaPrevista = result.DataEntregaPrevista,
                ClienteNome = result.ClienteNome,
                EnderecoNome = result.EnderecoNome,
                Logradouro = result.Logradouro,
                Numero = result.Numero,
                Complemento = result.Complemento,
                Bairro = result.Bairro,
                Cidade = result.Cidade,
                Estado = result.Estado,
                CEP = result.CEP,
                Subtotal = result.Subtotal,
                Frete = result.Frete,
                Desconto = result.Desconto,
                Total = result.Total,
                Itens = result.Itens.Select(item => new DetalhesPedidoItemViewModel {
                    PedidoItemId = item.PedidoItemId,
                    LivroId = item.LivroId,
                    Titulo = item.Titulo,
                    Autor = item.Autor,
                    ImagemUrl = item.ImagemUrl,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    PedidoEntregue = item.PedidoEntregue,
                    TrocaId = item.TrocaId,
                    TrocaStatus = item.TrocaStatus,
                    CodigoCupomTroca = item.CodigoCupomTroca,
                    ValorCupomTroca = item.ValorCupomTroca
                }).ToList(),
                Pagamentos = result.Pagamentos.Select(payment => new DetalhesPedidoPagamentoViewModel {
                    Metodo = payment.Metodo,
                    Valor = payment.Valor,
                    Status = payment.Status
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SolicitarTroca(int pedidoId, int pedidoItemId, int quantidadeSolicitada, string motivo, string? observacaoCliente) {
            var clienteId = ObterClienteId();
            if (clienteId == null) {
                return RedirectToAction("Login", "Auth", new {
                    returnUrl = Url.Action(nameof(DetalhesPedido), "Pedido", new { id = pedidoId })
                });
            }

            var result = _customerOrdersService.RequestExchange(new CustomerExchangeRequestCommand {
                CustomerId = clienteId.Value,
                OrderId = pedidoId,
                OrderItemId = pedidoItemId,
                QuantityRequested = quantidadeSolicitada,
                Reason = motivo,
                CustomerNote = observacaoCliente
            });

            if (!result.OrderItemFound) {
                TempData["ErroTroca"] = "Nao foi possivel localizar o item para solicitar a troca.";
                return RedirectToAction(nameof(DetalhesPedido), new { id = pedidoId });
            }

            if (!result.Success) {
                TempData["ErroTroca"] = result.ErrorMessage;
                return RedirectToAction(nameof(DetalhesPedido), new { id = pedidoId });
            }

            TempData["SucessoTroca"] = result.SuccessMessage;
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

        private CustomerCartSyncResult SincronizarCarrinhoComEstoque(bool renovarReservas) {
            var resultado = _customerCartService.Synchronize(new CustomerCartSyncCommand {
                Items = ObterCarrinhoDaSessao(),
                CustomerId = ObterClienteId(),
                SessionKey = ObterSessionKeyReserva(),
                RenewReservations = renovarReservas
            });

            if (resultado.CarrinhoMudou) {
                SalvarCarrinhoNaSessao(resultado.UpdatedItems);
            }

            return resultado;
        }

        private List<CustomerCartItemEntry> ObterCarrinhoDaSessao() {
            var carrinhoJson = HttpContext.Session.GetString(CarrinhoSessionKey);
            if (!string.IsNullOrWhiteSpace(carrinhoJson)) {
                return JsonSerializer.Deserialize<List<CustomerCartItemEntry>>(carrinhoJson) ?? new List<CustomerCartItemEntry>();
            }

            var clienteId = ObterClienteId();
            if (!clienteId.HasValue) {
                return new List<CustomerCartItemEntry>();
            }

            var itens = _customerCartService.LoadStoredCart(clienteId.Value);
            if (itens.Any()) {
                HttpContext.Session.SetString(CarrinhoSessionKey, JsonSerializer.Serialize(itens));
            }

            return itens;
        }

        private void SalvarCarrinhoNaSessao(List<CustomerCartItemEntry> itens) {
            if (itens == null || !itens.Any()) {
                HttpContext.Session.Remove(CarrinhoSessionKey);
                return;
            }

            var carrinhoJson = JsonSerializer.Serialize(itens);
            HttpContext.Session.SetString(CarrinhoSessionKey, carrinhoJson);
        }

        private CheckoutViewModel MontarCheckoutViewModel(int clienteId, CheckoutFormData form, CustomerCartSyncResult? sincronizacaoCarrinho = null) {
            var preparation = _customerCheckoutService.Prepare(CriarCheckoutPreparationRequest(clienteId, form, sincronizacaoCarrinho));
            AplicarPreparacaoAoFormulario(form, preparation);

            return new CheckoutViewModel {
                Livro = preparation.PrimeiroLivro,
                Itens = preparation.Items.Select(i => new CheckoutResumoItemViewModel {
                    LivroId = i.Livro.Id,
                    Titulo = i.Livro.Titulo,
                    Autor = i.Livro.Autor,
                    ImagemUrl = i.Livro.ImagemUrl,
                    PrecoUnitario = i.PrecoUnitario,
                    Quantidade = i.Quantidade
                }).ToList(),
                Enderecos = preparation.Enderecos,
                Cartoes = preparation.Cartoes,
                Bandeiras = preparation.Bandeiras,
                CuponsTrocaDisponiveis = preparation.CuponsTrocaDisponiveis.Select(c => new CheckoutCupomDisponivelViewModel {
                    Id = c.Id,
                    Codigo = c.Codigo,
                    Valor = c.Valor
                }).ToList(),
                Quantidade = preparation.QuantidadeTotal,
                Subtotal = preparation.Subtotal,
                Frete = preparation.Frete,
                Desconto = preparation.Desconto,
                Total = preparation.Total,
                OrigemCarrinho = form.UsarCarrinho,
                PermiteAlterarQuantidade = !form.UsarCarrinho && preparation.Items.Count == 1,
                RequerRevisaoCarrinho = preparation.RequiresCartReview,
                AvisosCarrinho = preparation.CartWarnings,
                Form = form
            };
        }

        private CustomerCheckoutPreparationRequest CriarCheckoutPreparationRequest(int clienteId, CheckoutFormData form, CustomerCartSyncResult? sincronizacaoCarrinho = null) {
            return new CustomerCheckoutPreparationRequest {
                CustomerId = clienteId,
                UseCart = form.UsarCarrinho,
                LivroId = form.LivroId,
                Quantidade = form.Quantidade,
                EnderecoId = form.EnderecoId > 0 ? form.EnderecoId : null,
                HasManualAddressData = !string.IsNullOrWhiteSpace(form.Logradouro),
                EstadoInformado = form.Estado,
                CodigoCupom = form.Cupom,
                CuponsTrocaSelecionados = form.CuponsTrocaSelecionados,
                TipoEntrega = form.TipoEntrega,
                DataEntregaPrevista = form.DataEntregaPrevista,
                CartSyncResult = sincronizacaoCarrinho
            };
        }

        private static void AplicarPreparacaoAoFormulario(CheckoutFormData form, CustomerCheckoutPreparationResult preparation) {
            form.EnderecoId = preparation.SelectedAddressId ?? 0;
            form.TipoEntrega = preparation.TipoEntrega;
            form.DataEntregaPrevista = preparation.DataEntregaPrevista;
            form.TipoLogradouro ??= preparation.DefaultTipoLogradouro;
            form.TipoResidencia ??= preparation.DefaultTipoResidencia;
            form.Pais ??= preparation.DefaultPais;
            form.CuponsTrocaSelecionados = preparation.AppliedExchangeCouponIds;
            form.Cupom = preparation.AppliedCouponCode ?? form.Cupom;

            if (preparation.QuantidadeTotal > 0) {
                form.Quantidade = preparation.QuantidadeTotal;
                if (preparation.PrimeiroLivro != null) {
                    form.LivroId = preparation.PrimeiroLivro.Id;
                }
            }
        }

        private string ObterSessionKeyReserva() {
            _ = HttpContext.Session.Id;
            return HttpContext.Session.Id;
        }

        private List<CheckoutPaymentSlot> ObterPagamentosCheckout(CheckoutFormData form) {
            return new List<CheckoutPaymentSlot> {
                new() {
                    Indice = 1,
                    Metodo = form.Metodo1?.Trim() ?? string.Empty,
                    Valor = form.Valor1 ?? 0,
                    CartaoId = form.CartaoId1,
                    BandeiraCartaoId = form.BandeiraCartaoId1,
                    SalvarNovoCartao = form.SalvarNovoCartao1,
                    NomeCartao = form.NomeCartao1,
                    NumeroCartao = form.NumeroCartao1,
                    CVV = form.CVV1,
                    Validade = form.Validade1
                },
                new() {
                    Indice = 2,
                    Metodo = form.Metodo2?.Trim() ?? string.Empty,
                    Valor = form.Valor2 ?? 0,
                    CartaoId = form.CartaoId2,
                    BandeiraCartaoId = form.BandeiraCartaoId2,
                    SalvarNovoCartao = form.SalvarNovoCartao2,
                    NomeCartao = form.NomeCartao2,
                    NumeroCartao = form.NumeroCartao2,
                    CVV = form.CVV2,
                    Validade = form.Validade2
                },
                new() {
                    Indice = 3,
                    Metodo = form.Metodo3?.Trim() ?? string.Empty,
                    Valor = form.Valor3 ?? 0,
                    CartaoId = form.CartaoId3,
                    BandeiraCartaoId = form.BandeiraCartaoId3,
                    SalvarNovoCartao = form.SalvarNovoCartao3,
                    NomeCartao = form.NomeCartao3,
                    NumeroCartao = form.NumeroCartao3,
                    CVV = form.CVV3,
                    Validade = form.Validade3
                },
                new() {
                    Indice = 4,
                    Metodo = form.Metodo4?.Trim() ?? string.Empty,
                    Valor = form.Valor4 ?? 0,
                    CartaoId = form.CartaoId4,
                    BandeiraCartaoId = form.BandeiraCartaoId4,
                    SalvarNovoCartao = form.SalvarNovoCartao4,
                    NomeCartao = form.NomeCartao4,
                    NumeroCartao = form.NumeroCartao4,
                    CVV = form.CVV4,
                    Validade = form.Validade4
                }
            };
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

            return OrderStatusHelper.NormalizeDisplayStatus(statusAtual, "Nao informado");
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

        private static string FormatarTipoEntrega(string? tipoEntrega) {
            return string.Equals(tipoEntrega, "PROGRAMADA", StringComparison.OrdinalIgnoreCase)
                ? "Entrega programada"
                : "Entrega padrão";
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
    }
}









