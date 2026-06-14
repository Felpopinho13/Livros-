using Livros.Domain;

namespace Livros.Application.AdminOrders {
    public sealed class AdminOrdersPageData {
        public List<Pedido> Pedidos { get; init; } = new();
        public int TotalPedidos { get; init; }
    }

    public sealed class AdminOrdersResult {
        public string? Busca { get; init; }
        public string? StatusFiltro { get; init; }
        public int PaginaAtual { get; init; }
        public int TotalPaginas { get; init; }
        public List<Pedido> Pedidos { get; init; } = new();
        public Dictionary<int, int> TrocasPorPedido { get; init; } = new();
    }
}
