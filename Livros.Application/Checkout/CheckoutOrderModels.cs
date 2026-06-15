namespace Livros.Application.Checkout {
    public sealed class CheckoutOrderItemInput {
        public int LivroId { get; init; }
        public int Quantidade { get; init; }
        public decimal PrecoUnitario { get; init; }
    }
    public sealed class CheckoutOrderBuildRequest {
        public int ClienteId { get; init; }
        public int EnderecoId { get; init; }
        public decimal Total { get; init; }
        public string TipoEntrega { get; init; } = "PADRAO";
        public DateTime? DataEntregaPrevista { get; init; }
        public IReadOnlyCollection<CheckoutOrderItemInput> Itens { get; init; } = Array.Empty<CheckoutOrderItemInput>();
    }
}
