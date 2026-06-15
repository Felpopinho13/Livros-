using Livros.Application.CustomerAccounts;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class CustomerAccountDataProvider : ICustomerAccountDataProvider {
        private static readonly string[] OpenExchangeStatusesToIgnore = {
            "TROCADO",
            "Recebida",
            "Aprovado",
            "TROCA RECUSADA",
            "Recusado"
        };

        private readonly AppDbContext _context;

        public CustomerAccountDataProvider(AppDbContext context) {
            _context = context;
        }

        public Cliente? LoadActiveCustomerByEmailWithAddressesAndCards(string email) {
            return _context.Clientes
                .Include(c => c.Enderecos)
                .Include(c => c.Cartoes)
                .FirstOrDefault(c => c.Email == email && c.IsAtivo);
        }

        public Cliente? LoadActiveCustomerByEmail(string email) {
            return _context.Clientes.FirstOrDefault(c => c.Email == email && c.IsAtivo);
        }

        public Cliente? LoadCustomerById(int customerId) {
            return _context.Clientes.FirstOrDefault(c => c.Id == customerId);
        }

        public List<Pedido> LoadOrdersByCustomerIdWithItemsAndBooks(int customerId) {
            return _context.Pedidos
                .Where(p => p.ClienteId == customerId)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .OrderByDescending(p => p.Data)
                .ToList();
        }

        public int CountOpenExchangesByCustomerId(int customerId) {
            return _context.Trocas.Count(t =>
                t.ClienteId == customerId &&
                !OpenExchangeStatusesToIgnore.Contains(t.Status));
        }

        public List<CupomDesconto> LoadCouponsByCustomerId(int customerId) {
            return _context.CuponsDesconto
                .Where(c => c.ClienteId == customerId)
                .ToList();
        }

        public bool EmailExistsForAnotherCustomer(string email, int customerId) {
            return _context.Clientes.Any(c => c.Email == email && c.Id != customerId);
        }

        public bool VerifyPassword(string plainTextPassword, string passwordHash) {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
        }

        public string HashPassword(string plainTextPassword) {
            return BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
