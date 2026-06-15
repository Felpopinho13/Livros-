using Livros.Application.CustomerCart;
using Livros.Domain;

namespace Livros.Application.CustomerCheckout {
    public sealed class CustomerCheckoutPreparationRequest {
        public int CustomerId { get; set; }
        public bool UseCart { get; set; }
        public int LivroId { get; set; }
        public int Quantidade { get; set; } = 1;
        public int? EnderecoId { get; set; }
        public bool HasManualAddressData { get; set; }
        public string? EstadoInformado { get; set; }
        public string? CodigoCupom { get; set; }
        public List<int> CuponsTrocaSelecionados { get; set; } = new();
        public string? TipoEntrega { get; set; }
        public DateTime? DataEntregaPrevista { get; set; }
        public CustomerCartSyncResult? CartSyncResult { get; set; }
    }

    public sealed class CustomerCheckoutPreparationResult {
        public List<CustomerCheckoutItemData> Items { get; set; } = new();
        public List<Endereco> Enderecos { get; set; } = new();
        public List<Cartao> Cartoes { get; set; } = new();
        public List<BandeiraCartao> Bandeiras { get; set; } = new();
        public List<CupomDesconto> CuponsTrocaDisponiveis { get; set; } = new();
        public int? SelectedAddressId { get; set; }
        public string TipoEntrega { get; set; } = "PADRAO";
        public DateTime? DataEntregaPrevista { get; set; }
        public string DefaultTipoLogradouro { get; set; } = "Rua";
        public string DefaultTipoResidencia { get; set; } = "Casa";
        public string DefaultPais { get; set; } = "Brasil";
        public string? AppliedCouponCode { get; set; }
        public List<int> AppliedExchangeCouponIds { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Frete { get; set; }
        public decimal Desconto { get; set; }
        public decimal Total { get; set; }
        public int QuantidadeTotal { get; set; }
        public Livro? PrimeiroLivro { get; set; }
        public bool RequiresCartReview { get; set; }
        public List<string> CartWarnings { get; set; } = new();
    }

    public sealed class CustomerCheckoutItemData {
        public Livro Livro { get; set; } = null!;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
    }
}
