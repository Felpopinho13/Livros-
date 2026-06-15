using Livros.Application.CustomerIdentity;
using Livros.Domain;
using Livros.Infrastructure.Data;

namespace Livros.Infrastructure.Services {
    public sealed class CustomerIdentityDataProvider : ICustomerIdentityDataProvider {
        private readonly AppDbContext _context;

        public CustomerIdentityDataProvider(AppDbContext context) {
            _context = context;
        }

        public List<Cliente> LoadActiveCustomers() {
            return _context.Clientes
                .Where(customer => customer.IsAtivo)
                .ToList();
        }

        public Cliente? LoadActiveCustomerByEmail(string email) {
            return _context.Clientes.FirstOrDefault(customer => customer.Email == email && customer.IsAtivo);
        }

        public bool VerifyPassword(string plainTextPassword, string passwordHash) {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
        }
    }
}
