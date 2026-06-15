using Livros.Application.Checkout;
using Livros.Application.CustomerCart;

namespace Livros.Application.CustomerCheckout {
    public sealed class CustomerCheckoutService {
        private readonly ICustomerCheckoutDataProvider _dataProvider;
        private readonly CheckoutPricingService _pricingService;

        public CustomerCheckoutService(ICustomerCheckoutDataProvider dataProvider, CheckoutPricingService pricingService) {
            _dataProvider = dataProvider;
            _pricingService = pricingService;
        }

        public CustomerCheckoutPreparationResult Prepare(CustomerCheckoutPreparationRequest request) {
            var items = ResolveItems(request);
            var addresses = _dataProvider.LoadDeliveryAddressesByCustomerId(request.CustomerId)
                .OrderByDescending(e => e.IsPadrao)
                .ThenBy(e => e.NomeEndereco)
                .ToList();
            var cards = _dataProvider.LoadCardsByCustomerIdWithBrand(request.CustomerId)
                .OrderByDescending(c => c.IsPadrao)
                .ToList();
            var brands = _dataProvider.LoadActiveBrands()
                .OrderBy(b => b.Nome)
                .ToList();
            var exchangeCoupons = _dataProvider.LoadAvailableExchangeCouponsByCustomerId(request.CustomerId)
                .OrderByDescending(c => c.DataCriacao)
                .ToList();

            var selectedAddressId = request.EnderecoId;
            if ((!selectedAddressId.HasValue || selectedAddressId.Value <= 0) && addresses.Any() && !request.HasManualAddressData) {
                selectedAddressId = (addresses.FirstOrDefault(e => e.IsPadrao) ?? addresses.First()).Id;
            }

            var deliveryType = NormalizeDeliveryType(request.TipoEntrega);
            var scheduledDate = NormalizeScheduledDate(deliveryType, request.DataEntregaPrevista);

            var subtotal = items.Sum(i => i.PrecoUnitario * i.Quantidade);
            var totalQuantity = items.Sum(i => i.Quantidade);
            var pricing = _pricingService.Calculate(new CheckoutPricingRequest {
                ClienteId = request.CustomerId,
                EnderecoId = selectedAddressId.HasValue && selectedAddressId.Value > 0 ? selectedAddressId : null,
                EstadoInformado = selectedAddressId.HasValue && selectedAddressId.Value > 0 ? null : request.EstadoInformado,
                Quantidade = totalQuantity,
                Subtotal = subtotal,
                CodigoCupom = request.CodigoCupom,
                CuponsTrocaSelecionados = request.CuponsTrocaSelecionados
            });

            return new CustomerCheckoutPreparationResult {
                Items = items,
                Enderecos = addresses,
                Cartoes = cards,
                Bandeiras = brands,
                CuponsTrocaDisponiveis = exchangeCoupons,
                SelectedAddressId = selectedAddressId,
                TipoEntrega = deliveryType,
                DataEntregaPrevista = scheduledDate,
                AppliedCouponCode = pricing.CodigoPromocionalAplicado ?? request.CodigoCupom,
                AppliedExchangeCouponIds = pricing.CuponsTrocaAplicados.Select(c => c.Id).ToList(),
                Subtotal = subtotal,
                Frete = pricing.Frete,
                Desconto = pricing.DescontoTotal,
                Total = Math.Max(subtotal + pricing.Frete - pricing.DescontoTotal, 0),
                QuantidadeTotal = totalQuantity,
                PrimeiroLivro = items.FirstOrDefault()?.Livro,
                RequiresCartReview = request.UseCart && (request.CartSyncResult?.RequerRevisao ?? false),
                CartWarnings = request.UseCart ? (request.CartSyncResult?.Avisos ?? new List<string>()) : new List<string>()
            };
        }

        public List<CustomerCheckoutItemData> ResolveItems(CustomerCheckoutPreparationRequest request) {
            if (request.UseCart) {
                var sync = request.CartSyncResult;
                return (sync?.Itens ?? new List<CustomerCartNormalizedItem>())
                    .Select(item => new CustomerCheckoutItemData {
                        Livro = item.Livro,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = item.Livro.Preco
                    })
                    .ToList();
            }

            var book = _dataProvider.LoadActiveBookWithStock(request.LivroId);
            if (book == null) {
                return new List<CustomerCheckoutItemData>();
            }

            return new List<CustomerCheckoutItemData> {
                new() {
                    Livro = book,
                    Quantidade = request.Quantidade <= 0 ? 1 : request.Quantidade,
                    PrecoUnitario = book.Preco
                }
            };
        }

        public List<string> ValidateStock(IEnumerable<CustomerCheckoutItemData> items) {
            var errors = new List<string>();

            foreach (var item in items) {
                var availableStock = item.Livro.Estoque?.Quantidade ?? 0;
                if (availableStock < item.Quantidade) {
                    errors.Add($"O livro \"{item.Livro.Titulo}\" nao possui estoque suficiente para concluir a compra.");
                }
            }

            return errors;
        }

        public string? TryDecreaseStock(IEnumerable<CustomerCheckoutItemData> items) {
            foreach (var item in items) {
                var stock = _dataProvider.LoadStockByBookId(item.Livro.Id);
                if (stock == null) {
                    return $"Nao foi encontrado estoque para o livro \"{item.Livro.Titulo}\".";
                }

                if (stock.Quantidade < item.Quantidade) {
                    return $"Estoque insuficiente para o livro \"{item.Livro.Titulo}\". Disponivel: {stock.Quantidade}.";
                }
            }

            foreach (var item in items) {
                var stock = _dataProvider.LoadStockByBookId(item.Livro.Id)!;
                stock.Quantidade -= item.Quantidade;
            }

            _dataProvider.SaveChanges();
            return null;
        }

        public static string NormalizeDeliveryType(string? deliveryType) {
            if (string.Equals(deliveryType, "PROGRAMADA", StringComparison.OrdinalIgnoreCase)) {
                return "PROGRAMADA";
            }

            return "PADRAO";
        }

        public static DateTime? NormalizeScheduledDate(string? deliveryType, DateTime? scheduledDate) {
            if (!string.Equals(deliveryType, "PROGRAMADA", StringComparison.OrdinalIgnoreCase)) {
                return null;
            }

            return scheduledDate?.Date;
        }

        public static DateTime GetMinimumScheduledDate() {
            return DateTime.Today.AddDays(7);
        }
    }
}
