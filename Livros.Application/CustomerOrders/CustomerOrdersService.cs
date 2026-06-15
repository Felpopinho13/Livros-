using Livros.Application.AdminOrders;
using Livros.Domain;

namespace Livros.Application.CustomerOrders {
    public sealed class CustomerOrdersService {
        private readonly ICustomerOrdersDataProvider _dataProvider;

        public CustomerOrdersService(ICustomerOrdersDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public CustomerOrderConfirmationResult GetConfirmation(CustomerOrderConfirmationQuery query) {
            var order = _dataProvider.LoadOrderWithItemsAndBooks(query.OrderId, query.CustomerId);
            if (order == null) {
                return new CustomerOrderConfirmationResult {
                    OrderFound = false
                };
            }

            var mainItem = order.Itens.FirstOrDefault();
            var exchanges = _dataProvider.LoadExchangesByOrderIds(new[] { order.Id });

            return new CustomerOrderConfirmationResult {
                OrderFound = true,
                PedidoId = order.Id,
                Status = FormatOrderStatus(order.Status, exchanges),
                TipoEntrega = FormatDeliveryType(order.TipoEntrega),
                DataEntregaPrevista = order.DataEntregaPrevista,
                Total = order.Total,
                LivroTitulo = mainItem?.Livro?.Titulo ?? "Pedido",
                Quantidade = order.Itens.Sum(i => i.Quantidade)
            };
        }

        public CustomerOrdersResult List(CustomerOrdersQuery query) {
            var orders = _dataProvider.LoadOrdersByCustomerIdWithItemsAndBooks(query.CustomerId);
            var exchangesByOrderId = _dataProvider.LoadExchangesByOrderIds(orders.Select(o => o.Id))
                .GroupBy(t => t.PedidoId)
                .ToDictionary(group => group.Key, group => group.ToList());

            return new CustomerOrdersResult {
                Orders = orders.Select(order => {
                    var mainItem = order.Itens.FirstOrDefault();
                    return new CustomerOrderSummaryData {
                        PedidoId = order.Id,
                        Data = order.Data,
                        Total = order.Total,
                        Status = FormatOrderStatus(order.Status, exchangesByOrderId.TryGetValue(order.Id, out var exchanges) ? exchanges : null),
                        TipoEntrega = FormatDeliveryType(order.TipoEntrega),
                        DataEntregaPrevista = order.DataEntregaPrevista,
                        LivroTitulo = mainItem?.Livro?.Titulo ?? "Pedido sem itens",
                        LivroAutor = mainItem?.Livro?.Autor ?? string.Empty,
                        LivroImagemUrl = mainItem?.Livro?.ImagemUrl ?? string.Empty,
                        QuantidadeItens = order.Itens.Count,
                        QuantidadeLivros = order.Itens.Sum(i => i.Quantidade),
                        LivroIdPrincipal = mainItem?.LivroId ?? 0
                    };
                }).ToList()
            };
        }

        public CustomerOrderDetailsResult GetDetails(CustomerOrderDetailsQuery query) {
            var order = _dataProvider.LoadDetailedOrder(query.OrderId, query.CustomerId);
            if (order == null) {
                return new CustomerOrderDetailsResult {
                    OrderFound = false
                };
            }

            var exchanges = _dataProvider.LoadExchangesByOrderIdWithCoupon(order.Id);
            var subtotal = order.Itens.Sum(i => i.PrecoUnitario * i.Quantidade);
            var orderCoupons = _dataProvider.LoadCouponsByOrderId(order.Id);
            var discount = orderCoupons.Sum(c => c.Valor);
            var shipping = Math.Max(order.Total - subtotal + discount, 0);
            var displayStatus = FormatOrderStatus(order.Status, exchanges);

            return new CustomerOrderDetailsResult {
                OrderFound = true,
                PedidoId = order.Id,
                Data = order.Data,
                Status = displayStatus,
                TipoEntrega = FormatDeliveryType(order.TipoEntrega),
                DataEntregaPrevista = order.DataEntregaPrevista,
                ClienteNome = order.Cliente?.Nome ?? string.Empty,
                EnderecoNome = order.Endereco?.NomeEndereco ?? string.Empty,
                Logradouro = order.Endereco?.Logradouro ?? string.Empty,
                Numero = order.Endereco?.Numero ?? string.Empty,
                Complemento = order.Endereco?.Complemento ?? string.Empty,
                Bairro = order.Endereco?.Bairro?.Nome ?? string.Empty,
                Cidade = order.Endereco?.Cidade?.Nome ?? string.Empty,
                Estado = order.Endereco?.Cidade?.Estado?.Sigla ?? string.Empty,
                CEP = order.Endereco?.CEP ?? string.Empty,
                Subtotal = subtotal,
                Frete = shipping,
                Desconto = discount,
                Total = order.Total,
                Itens = order.Itens.Select(item => {
                    var exchange = exchanges.FirstOrDefault(t => t.PedidoItemId == item.Id);
                    return new CustomerOrderDetailItemData {
                        PedidoItemId = item.Id,
                        LivroId = item.LivroId,
                        Titulo = item.Livro?.Titulo ?? string.Empty,
                        Autor = item.Livro?.Autor ?? string.Empty,
                        ImagemUrl = item.Livro?.ImagemUrl ?? string.Empty,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = item.PrecoUnitario,
                        PedidoEntregue = displayStatus == "ENTREGUE",
                        TrocaId = exchange?.Id,
                        TrocaStatus = NormalizeExchangeDisplayStatus(exchange),
                        CodigoCupomTroca = exchange?.CupomDesconto?.Codigo,
                        ValorCupomTroca = exchange?.CupomDesconto?.Valor
                    };
                }).ToList(),
                Pagamentos = order.Pagamentos.Select(payment => new CustomerOrderPaymentData {
                    Metodo = FormatPaymentMethod(payment.Metodo),
                    Valor = payment.Valor,
                    Status = payment.Status
                }).ToList()
            };
        }

        public CustomerExchangeRequestResult RequestExchange(CustomerExchangeRequestCommand command) {
            var orderItem = _dataProvider.LoadOrderItemForExchange(command.OrderItemId, command.OrderId, command.CustomerId);
            if (orderItem == null) {
                return Failure("Nao foi possivel localizar o item para solicitar a troca.", found: false);
            }

            var displayStatus = OrderStatusHelper.NormalizeDisplayStatus(orderItem.Pedido?.Status, "Nao informado");
            if (displayStatus != "ENTREGUE") {
                return Failure("A troca so pode ser solicitada para pedidos ENTREGUE.");
            }

            if (_dataProvider.LoadExchangeByOrderItemId(command.OrderItemId) != null) {
                return Failure("Ja existe uma solicitacao de troca para este item.");
            }

            if (string.IsNullOrWhiteSpace(command.Reason)) {
                return Failure("Selecione ou informe um motivo para solicitar a troca.");
            }

            if (command.QuantityRequested < 1) {
                return Failure("Informe uma quantidade valida para solicitar a troca.");
            }

            if (command.QuantityRequested > orderItem.Quantidade) {
                return Failure("A quantidade solicitada para troca nao pode ser maior que a quantidade comprada.");
            }

            var exchangeOrderItem = orderItem;
            if (command.QuantityRequested < orderItem.Quantidade) {
                orderItem.Quantidade -= command.QuantityRequested;

                exchangeOrderItem = new PedidoItem {
                    PedidoId = orderItem.PedidoId,
                    LivroId = orderItem.LivroId,
                    Quantidade = command.QuantityRequested,
                    PrecoUnitario = orderItem.PrecoUnitario
                };

                _dataProvider.AddOrderItem(exchangeOrderItem);
                _dataProvider.SaveChanges();
            }

            var exchange = new Troca {
                Codigo = GenerateExchangeCode(),
                PedidoId = command.OrderId,
                PedidoItemId = exchangeOrderItem.Id,
                ClienteId = command.CustomerId,
                Motivo = command.Reason.Trim(),
                ObservacaoCliente = command.CustomerNote?.Trim(),
                Status = "EM TROCA",
                DataSolicitacao = DateTime.Now
            };

            _dataProvider.AddExchange(exchange);
            _dataProvider.SaveChanges();

            return new CustomerExchangeRequestResult {
                OrderItemFound = true,
                Success = true,
                SuccessMessage = $"Solicitacao de troca de {command.QuantityRequested} unidade(s) do livro \"{orderItem.Livro?.Titulo}\" enviada com sucesso."
            };
        }

        private static string GenerateExchangeCode() {
            return $"SOL-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private static string FormatPaymentMethod(string? method) {
            if (string.IsNullOrWhiteSpace(method)) {
                return "Nao informado";
            }

            return method.Trim().ToLowerInvariant() switch {
                "cartao" => "Cartao",
                "pix" => "Pix",
                "boleto" => "Boleto",
                _ => method
            };
        }

        private static string FormatOrderStatus(string? currentStatus, IEnumerable<Troca>? exchanges = null) {
            if (exchanges != null && exchanges.Any(CompletedExchangeForCustomer)) {
                return "Troca efetuada";
            }

            return OrderStatusHelper.NormalizeDisplayStatus(currentStatus, "Nao informado");
        }

        private static bool CompletedExchangeForCustomer(Troca exchange) {
            if (exchange == null) {
                return false;
            }

            if (string.Equals(exchange.Status, "TROCADO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(exchange.Status, "Recebida", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            return string.Equals(exchange.Status, "Aprovado", StringComparison.OrdinalIgnoreCase)
                && exchange.CupomDescontoId.HasValue;
        }

        private static string? NormalizeExchangeDisplayStatus(Troca? exchange) {
            if (exchange == null) {
                return null;
            }

            if (string.Equals(exchange.Status, "TROCADO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(exchange.Status, "Recebida", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(exchange.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) && exchange.CupomDescontoId.HasValue)) {
                return "TROCADO";
            }

            if (string.Equals(exchange.Status, "TROCA AUTORIZADA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(exchange.Status, "Autorizada", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(exchange.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) && !exchange.CupomDescontoId.HasValue)) {
                return "TROCA AUTORIZADA";
            }

            if (string.Equals(exchange.Status, "TROCA RECUSADA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(exchange.Status, "Recusado", StringComparison.OrdinalIgnoreCase)) {
                return "TROCA RECUSADA";
            }

            if (string.Equals(exchange.Status, "EM TROCA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(exchange.Status, "Solicitado", StringComparison.OrdinalIgnoreCase)) {
                return "EM TROCA";
            }

            return exchange.Status;
        }

        private static string FormatDeliveryType(string? deliveryType) {
            return string.Equals(deliveryType, "PROGRAMADA", StringComparison.OrdinalIgnoreCase)
                ? "Entrega programada"
                : "Entrega padrão";
        }

        private static CustomerExchangeRequestResult Failure(string message, bool found = true) {
            return new CustomerExchangeRequestResult {
                OrderItemFound = found,
                Success = false,
                ErrorMessage = message
            };
        }
    }
}
