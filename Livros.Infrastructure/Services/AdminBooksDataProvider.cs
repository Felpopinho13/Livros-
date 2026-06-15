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

        public List<Livro> LoadBooksWithStockAndCategories(AdminBooksCatalogQuery query) {
            var livros = _context.Livros
                .Include(l => l.Estoque)
                .Include(l => l.Categorias)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Busca)) {
                var busca = query.Busca.Trim().ToLower();
                livros = livros.Where(l =>
                    l.Titulo.ToLower().Contains(busca) ||
                    (l.Autor != null && l.Autor.ToLower().Contains(busca)) ||
                    (l.ISBN != null && l.ISBN.Contains(query.Busca)) ||
                    l.Categorias.Any(c => c.Nome.ToLower().Contains(busca)));
            }

            if (query.CategoriaId.HasValue && query.CategoriaId.Value > 0) {
                livros = livros.Where(l => l.Categorias.Any(c => c.Id == query.CategoriaId.Value));
            }

            livros = (query.Status ?? "ativos") switch {
                "todos" => livros,
                "inativos" => livros.Where(l => !l.IsAtivo),
                _ => livros.Where(l => l.IsAtivo)
            };

            return livros
                .OrderBy(l => l.Titulo)
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
