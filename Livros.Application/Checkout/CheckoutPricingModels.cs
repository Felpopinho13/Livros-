using Livros.Domain;
namespace Livros.Application.Checkout {
    public sealed class CheckoutCouponApplicationRequest {
        public int ClienteId { get; init; }
        public string? CodigoCupom { get; init; }
        public IReadOnlyCollection<int> CuponsTrocaSelecionados { get; init; } = Array.Empty<int>();
        public decimal Subtotal { get; init; }
        public decimal Frete { get; init; }
    }
    public sealed class CheckoutShippingRequest {
        public int ClienteId { get; init; }
        public int? EnderecoId { get; init; }
        public string? EstadoInformado { get; init; }
        public int Quantidade { get; init; } = 1;
    }
    public sealed class CheckoutPricingRequest {
        public int ClienteId { get; init; }
        public int? EnderecoId { get; init; }
        public string? EstadoInformado { get; init; }
        public int Quantidade { get; init; } = 1;
        public decimal Subtotal { get; init; }
        public string? CodigoCupom { get; init; }
        public IReadOnlyCollection<int> CuponsTrocaSelecionados { get; init; } = Array.Empty<int>();
    }
    public sealed class CheckoutShippingResult {
        public string EstadoDestino { get; init; } = "SP";
        public decimal Frete { get; init; }
    }
    public sealed class CheckoutPricingResult {
        public string EstadoDestino { get; init; } = "SP";
        public decimal Frete { get; init; }
        public CupomDesconto? CupomPromocional { get; init; }
        public string? CodigoPromocionalAplicado { get; init; }
        public List<CupomDesconto> CuponsTrocaAplicados { get; init; } = new();
        public decimal DescontoPromocional { get; init; }
        public decimal DescontoTroca { get; init; }
        public string? Mensagem { get; init; }
        public decimal DescontoTotal => decimal.Round(DescontoPromocional + DescontoTroca, 2);
    }
}
