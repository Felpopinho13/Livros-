using Livros.Domain;

namespace Livros.Application.CustomerIdentity {
    public interface ICustomerIdentityDataProvider {
        List<Cliente> LoadActiveCustomers();
        Cliente? LoadActiveCustomerByEmail(string email);
        bool VerifyPassword(string plainTextPassword, string passwordHash);
    }
}
