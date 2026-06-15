using Livros.Application.Checkout;

namespace Livros.Application.CustomerCheckout {
    public sealed class CustomerOrderPlacementRequest {
        public int CustomerId { get; set; }
        public bool UseCart { get; set; }
        public string SessionKey { get; set; } = string.Empty;
        public string DeliveryType { get; set; } = "PADRAO";
        public DateTime? ScheduledDeliveryDate { get; set; }
        public int EnderecoId { get; set; }
        public bool SaveNewAddress { get; set; }
        public string? NomeEndereco { get; set; }
        public string? CEP { get; set; }
        public string? TipoLogradouro { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? TipoResidencia { get; set; }
        public string? Pais { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? CouponCode { get; set; }
        public List<int> ExchangeCouponIds { get; set; } = new();
        public List<CustomerCheckoutItemData> Items { get; set; } = new();
        public List<CheckoutPaymentSlot> Payments { get; set; } = new();
    }

    public sealed class CustomerOrderPlacementResult {
        public bool Success { get; set; }
        public int? OrderId { get; set; }
        public int? ResolvedAddressId { get; set; }
        public string? AppliedCouponCode { get; set; }
        public List<int> AppliedExchangeCouponIds { get; set; } = new();
        public List<CustomerOrderPlacementValidationError> Errors { get; set; } = new();
    }

    public sealed class CustomerOrderPlacementValidationError {
        public string? Key { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
