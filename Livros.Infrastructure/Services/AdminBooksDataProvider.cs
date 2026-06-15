using Livros.Application.AdminBooks;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class AdminBooksDataProvider : IAdminBooksDataProvider {
        private readonly AppDbContext _context;

        public AdminBooksDataProvider(AppDbContext context) {
            _context = context;
        }

        public List<Categoria> LoadCategories() {
            return _context.Categorias
                .OrderBy(c => c.Nome)
                .ToList();
        }

        public List<Categoria> LoadCategoriesByIds(IReadOnlyCollection<int> categoryIds) {
            if (categoryIds.Count == 0) {
                return new List<Categoria>();
            }

            return _context.Categorias
                .Where(c => categoryIds.Contains(c.Id))
                .ToList();
        }

        public List<Livro> LoadActiveBooksWithStockAndCategories() {
            return _context.Livros
                .Include(l => l.Estoque)
                .Include(l => l.Categorias)
                .Where(l => l.IsAtivo)
                .ToList();
        }

        public Livro? LoadBookByIdWithCategories(int livroId) {
            return _context.Livros
                .Include(l => l.Categorias)
                .FirstOrDefault(l => l.Id == livroId);
        }

        public void AddBook(Livro livro) {
            _context.Livros.Add(livro);
        }

        public void AddStock(Estoque estoque) {
            _context.Estoques.Add(estoque);
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
