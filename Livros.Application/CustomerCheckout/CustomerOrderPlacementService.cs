using Livros.Application.Checkout;
using Livros.Application.CustomerCart;

namespace Livros.Application.CustomerCheckout {
    public sealed class CustomerOrderPlacementService {
        private readonly ICustomerOrderPlacementDataProvider _dataProvider;
        private readonly CheckoutAddressService _addressService;
        private readonly CheckoutPricingService _pricingService;
        private readonly CheckoutPaymentService _paymentService;
        private readonly CheckoutOrderService _orderService;
        private readonly CustomerCheckoutService _customerCheckoutService;
        private readonly CustomerCartService _customerCartService;

        public CustomerOrderPlacementService(
            ICustomerOrderPlacementDataProvider dataProvider,
            CheckoutAddressService addressService,
            CheckoutPricingService pricingService,
            CheckoutPaymentService paymentService,
            CheckoutOrderService orderService,
            CustomerCheckoutService customerCheckoutService,
            CustomerCartService customerCartService) {
            _dataProvider = dataProvider;
            _addressService = addressService;
            _pricingService = pricingService;
            _paymentService = paymentService;
            _orderService = orderService;
            _customerCheckoutService = customerCheckoutService;
            _customerCartService = customerCartService;
        }

        public CustomerOrderPlacementResult PlaceOrder(CustomerOrderPlacementRequest request) {
            var result = new CustomerOrderPlacementResult();

            foreach (var stockError in _customerCheckoutService.ValidateStock(request.Items)) {
                result.Errors.Add(new CustomerOrderPlacementValidationError {
                    Message = stockError
                });
            }

            var addressResolution = _addressService.Resolve(new CheckoutAddressResolutionRequest {
                ClienteId = request.CustomerId,
                EnderecoId = request.EnderecoId,
                NomeEndereco = request.NomeEndereco,
                CEP = request.CEP,
                TipoLogradouro = request.TipoLogradouro,
                Logradouro = request.Logradouro,
                Numero = request.Numero,
                Complemento = request.Complemento,
                TipoResidencia = request.TipoResidencia,
                Pais = request.Pais,
                Bairro = request.Bairro,
                Cidade = request.Cidade,
                Estado = request.Estado,
                SalvarNoPerfil = request.SaveNewAddress
            });

            foreach (var addressError in addressResolution.Errors) {
                result.Errors.Add(new CustomerOrderPlacementValidationError {
                    Message = addressError
                });
            }

            result.ResolvedAddressId = addressResolution.EnderecoId;

            var subtotal = request.Items.Sum(i => i.PrecoUnitario * i.Quantidade);
            var totalQuantity = request.Items.Sum(i => i.Quantidade);
            var pricing = _pricingService.Calculate(new CheckoutPricingRequest {
                ClienteId = request.CustomerId,
                EnderecoId = result.ResolvedAddressId,
                EstadoInformado = result.ResolvedAddressId.HasValue ? null : request.Estado,
                Quantidade = totalQuantity,
                Subtotal = subtotal,
                CodigoCupom = request.CouponCode,
                CuponsTrocaSelecionados = request.ExchangeCouponIds
            });

            result.AppliedCouponCode = pricing.CodigoPromocionalAplicado ?? request.CouponCode;
            result.AppliedExchangeCouponIds = pricing.CuponsTrocaAplicados.Select(c => c.Id).ToList();

            var total = Math.Max(subtotal + pricing.Frete - pricing.DescontoTotal, 0);

            if (string.Equals(request.DeliveryType, "PROGRAMADA", StringComparison.OrdinalIgnoreCase)) {
                var minimumScheduledDate = CustomerCheckoutService.GetMinimumScheduledDate();

                if (!request.ScheduledDeliveryDate.HasValue) {
                    result.Errors.Add(new CustomerOrderPlacementValidationError {
                        Key = "DataEntregaPrevista",
                        Message = "Informe a data prevista para a entrega programada."
                    });
                }
                else if (request.ScheduledDeliveryDate.Value.Date < minimumScheduledDate) {
                    result.Errors.Add(new CustomerOrderPlacementValidationError {
                        Key = "DataEntregaPrevista",
                        Message = $"A entrega programada deve ser agendada para {minimumScheduledDate:dd/MM/yyyy} ou uma data posterior."
                    });
                }
            }

            foreach (var paymentError in _paymentService.Validate(new CheckoutPaymentValidationRequest {
                ClienteId = request.CustomerId,
                Total = total,
                PermitirTotalZeroPorCupom = pricing.DescontoTotal > 0,
                Pagamentos = request.Payments
            })) {
                result.Errors.Add(new CustomerOrderPlacementValidationError {
                    Message = paymentError
                });
            }

            if (result.Errors.Any() || !result.ResolvedAddressId.HasValue) {
                return result;
            }

            var stockReductionError = _customerCheckoutService.TryDecreaseStock(request.Items);
            if (!string.IsNullOrWhiteSpace(stockReductionError)) {
                result.Errors.Add(new CustomerOrderPlacementValidationError {
                    Message = stockReductionError
                });
                return result;
            }

            var order = _orderService.Build(new CheckoutOrderBuildRequest {
                ClienteId = request.CustomerId,
                EnderecoId = result.ResolvedAddressId.Value,
                Total = total,
                TipoEntrega = request.DeliveryType,
                DataEntregaPrevista = request.ScheduledDeliveryDate,
                Itens = request.Items.Select(item => new CheckoutOrderItemInput {
                    LivroId = item.Livro.Id,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario
                }).ToList()
            });

            _paymentService.AppendPaymentsToOrder(request.CustomerId, request.Payments, order);

            _dataProvider.AddOrder(order);
            _dataProvider.SaveChanges();

            if (pricing.CupomPromocional != null || pricing.CuponsTrocaAplicados.Any()) {
                _pricingService.MarkAppliedCouponsAsUsed(order, pricing);
                _dataProvider.SaveChanges();
            }

            if (request.UseCart) {
                _customerCartService.Clear(new CustomerCartClearCommand {
                    CustomerId = request.CustomerId,
                    SessionKey = request.SessionKey
                });
            }

            result.Success = true;
            result.OrderId = order.Id;
            return result;
        }
    }
}
