using Livros.Domain;
namespace Livros.Application.Checkout {
    public sealed class CheckoutPaymentSlot {
        public int Indice { get; init; }
        public string Metodo { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public int? CartaoId { get; init; }
        public int? BandeiraCartaoId { get; init; }
        public bool SalvarNovoCartao { get; init; }
        public string? NomeCartao { get; init; }
        public string? NumeroCartao { get; init; }
        public string? CVV { get; init; }
        public string? Validade { get; init; }
    }
    public sealed class CheckoutPaymentValidationRequest {
        public int ClienteId { get; init; }
        public decimal Total { get; init; }
        public bool PermitirTotalZeroPorCupom { get; init; }
        public IReadOnlyCollection<CheckoutPaymentSlot> Pagamentos { get; init; } = Array.Empty<CheckoutPaymentSlot>();
    }
}
