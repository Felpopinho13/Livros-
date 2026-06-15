using Livros.Domain;

namespace Livros.Application.CustomerAccounts {
    public interface ICustomerAccountDataProvider {
        Cliente? LoadActiveCustomerByEmailWithAddressesAndCards(string email);
        Cliente? LoadActiveCustomerByEmail(string email);
        Cliente? LoadCustomerById(int customerId);
        List<Pedido> LoadOrdersByCustomerIdWithItemsAndBooks(int customerId);
        int CountOpenExchangesByCustomerId(int customerId);
        List<CupomDesconto> LoadCouponsByCustomerId(int customerId);
        bool EmailExistsForAnotherCustomer(string email, int customerId);
        bool VerifyPassword(string plainTextPassword, string passwordHash);
        string HashPassword(string plainTextPassword);
        void SaveChanges();
    }
}
