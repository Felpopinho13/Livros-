namespace Livros.Application.AdminOrders {
    public sealed class AdminOrdersQuery {
        public string? Busca { get; init; }
        public string? Status { get; init; }
        public int Pagina { get; init; } = 1;
    }
}
