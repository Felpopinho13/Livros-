using Livros.Application.AdminInventory;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class AdminInventoryDataProvider : IAdminInventoryDataProvider {
        private readonly AppDbContext _context;

        public AdminInventoryDataProvider(AppDbContext context) {
            _context = context;
        }

        public List<Livro> LoadActiveBooksWithoutStockRecord() {
            return _context.Livros
                .Where(book => book.IsAtivo && !_context.Estoques.Any(stock => stock.LivroId == book.Id))
                .ToList();
        }

        public void AddStocks(IEnumerable<Estoque> stocks) {
            _context.Estoques.AddRange(stocks);
        }

        public List<Estoque> LoadActiveInventoryWithBooks() {
            return _context.Estoques
                .Include(stock => stock.Livro)
                .Where(stock => stock.Livro.IsAtivo)
                .ToList();
        }

        public Estoque? LoadStockByBookId(int livroId) {
            return _context.Estoques.FirstOrDefault(stock => stock.LivroId == livroId);
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
