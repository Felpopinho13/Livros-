using Livros.Domain;

namespace Livros.Application.AdminExchanges {
    public interface IAdminExchangesDataProvider {
        Task<AdminExchangesPageData> LoadPageAsync(AdminExchangesQuery query, int pageSize, CancellationToken cancellationToken = default);
        Task<Troca?> GetTradeForUpdateAsync(int trocaId, CancellationToken cancellationToken = default);
        Task<decimal> CalculateSuggestedCouponValueAsync(Troca troca, CancellationToken cancellationToken = default);
        Task ReintegrateTradeItemToStockAsync(PedidoItem? pedidoItem, CancellationToken cancellationToken = default);
        Task<CupomDesconto> CreateCouponAsync(CupomDesconto cupom, CancellationToken cancellationToken = default);
        Task CreateCouponsAsync(IEnumerable<CupomDesconto> cupons, CancellationToken cancellationToken = default);
        Task<List<Cliente>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);
        Task<Cliente?> GetActiveCustomerAsync(int clienteId, CancellationToken cancellationToken = default);
        Task<CupomDesconto?> GetCouponAsync(int cupomId, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
