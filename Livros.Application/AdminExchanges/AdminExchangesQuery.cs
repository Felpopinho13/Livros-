namespace Livros.Application.AdminExchanges {
    public sealed class AdminExchangesQuery {
        public string? Busca { get; init; }
        public string? Status { get; init; }
        public int PaginaTrocas { get; init; } = 1;
        public int PaginaCupons { get; init; } = 1;
    }
}
