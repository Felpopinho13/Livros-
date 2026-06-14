using Livros.Domain;

namespace Livros.Application.AdminOrders {
    public interface IAdminOrdersDataProvider {
        Task<AdminOrdersPageData> LoadPageAsync(AdminOrdersQuery query, int pageSize, CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> LoadTradeCountsAsync(IReadOnlyCollection<int> pedidoIds, CancellationToken cancellationToken = default);
        Task<Pedido?> LoadForStatusUpdateAsync(int pedidoId, CancellationToken cancellationToken = default);
        Task<Dictionary<int, Estoque>> LoadStocksForBooksAsync(IReadOnlyCollection<int> livroIds, CancellationToken cancellationToken = default);
        Estoque CreateStock(int livroId);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}