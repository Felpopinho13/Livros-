using Livros.Domain;

namespace Livros.Application.AdminCustomers {
    public interface IAdminCustomersDataProvider {
        Task<AdminCustomersPageData> LoadPageAsync(AdminCustomersQuery query, int pageSize, CancellationToken cancellationToken = default);
        Task<Dictionary<int, decimal>> LoadEligibleTotalsAsync(IReadOnlyCollection<int> clienteIds, CancellationToken cancellationToken = default);
        Task<AdminCustomerTransactionsData> LoadTransactionsAsync(int clienteId, CancellationToken cancellationToken = default);
        Cliente? LoadCustomerById(int clienteId);
        Cliente? LoadCustomerByIdWithAddressesAndCards(int clienteId);
        bool HasDeletionDependencies(int clienteId);
        void AddCustomer(Cliente cliente);
        void RemoveCustomer(Cliente cliente);
        void RemoveAddresses(IEnumerable<Endereco> enderecos);
        void RemoveCards(IEnumerable<Cartao> cartoes);
        string HashPassword(string plainTextPassword);
        void SaveChanges();
    }
}
