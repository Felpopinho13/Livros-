using Livros.Application.CustomerCheckout;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class CustomerCheckoutDataProvider : ICustomerCheckoutDataProvider {
        private readonly AppDbContext _context;

        public CustomerCheckoutDataProvider(AppDbContext context) {
            _context = context;
        }

        public Livro? LoadActiveBookWithStock(int bookId) {
            return _context.Livros
                .Include(l => l.Estoque)
                .FirstOrDefault(l => l.Id == bookId && l.IsAtivo);
        }

        public List<Endereco> LoadDeliveryAddressesByCustomerId(int customerId) {
            return _context.Enderecos
                .Include(e => e.Bairro)
                .Include(e => e.Cidade)
                    .ThenInclude(c => c.Estado)
                .Where(e => e.ClienteId == customerId && e.IsEntrega)
                .ToList();
        }

        public List<Cartao> LoadCardsByCustomerIdWithBrand(int customerId) {
            return _context.Cartoes
                .Include(c => c.BandeiraCartao)
                .Where(c => c.ClienteId == customerId)
                .ToList();
        }

        public List<BandeiraCartao> LoadActiveBrands() {
            return _context.BandeirasCartao
                .Where(b => b.IsAtiva)
                .ToList();
        }

        public List<CupomDesconto> LoadAvailableExchangeCouponsByCustomerId(int customerId) {
            return _context.CuponsDesconto
                .Where(c => c.IsAtivo
                    && c.DataUtilizacao == null
                    && c.ClienteId == customerId
                    && c.Tipo == "TROCA")
                .ToList();
        }

        public Estoque? LoadStockByBookId(int bookId) {
            return _context.Estoques.FirstOrDefault(e => e.LivroId == bookId);
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
