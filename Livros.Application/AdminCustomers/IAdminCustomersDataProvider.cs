namespace Livros.Application.AdminCustomers {
    public interface IAdminCustomersDataProvider {
        Task<AdminCustomersPageData> LoadPageAsync(AdminCustomersQuery query, int pageSize, CancellationToken cancellationToken = default);
        Task<Dictionary<int, decimal>> LoadEligibleTotalsAsync(IReadOnlyCollection<int> clienteIds, CancellationToken cancellationToken = default);
        Task<AdminCustomerTransactionsData> LoadTransactionsAsync(int clienteId, CancellationToken cancellationToken = default);
    }
}
