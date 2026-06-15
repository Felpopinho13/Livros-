using Livros.Domain;

namespace Livros.Application.AdminSalesHistory {
    public interface IAdminSalesHistorySeedDataProvider {
        Task<List<Livro>> LoadEligibleBooksAsync(CancellationToken cancellationToken = default);
        Task<AdminSalesSeedGeography> EnsureGeographyAsync(CancellationToken cancellationToken = default);
        Task<List<Cliente>> EnsureCustomersAsync(AdminSalesSeedGeography geography, IReadOnlyCollection<string> demoCustomerNames, CancellationToken cancellationToken = default);
        void AddOrder(Pedido pedido);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
