using Livros.Application.Catalog;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class CatalogDataProvider : ICatalogDataProvider {
        private readonly AppDbContext _context;

        public CatalogDataProvider(AppDbContext context) {
            _context = context;
        }

        public List<Livro> LoadActiveBooksWithStockAndCategories(CatalogListQuery query) {
            var livros = _context.Livros
                .Include(l => l.Estoque)
                .Include(l => l.Categorias)
                .Where(l => l.IsAtivo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Busca)) {
                var busca = query.Busca.Trim().ToLower();
                livros = livros.Where(l =>
                    l.Titulo.ToLower().Contains(busca) ||
                    (l.Autor != null && l.Autor.ToLower().Contains(busca)) ||
                    (l.ISBN != null && l.ISBN.Contains(query.Busca)) ||
                    l.Categorias.Any(c => c.Nome.ToLower().Contains(busca)));
            }

            return livros
                .OrderBy(l => l.Titulo)
                .ToList();
        }

        public Livro? LoadActiveBookByIdWithStockAndCategories(int id) {
            return _context.Livros
                .Include(l => l.Estoque)
                .Include(l => l.Categorias)
                .FirstOrDefault(l => l.Id == id && l.IsAtivo);
        }

        public List<Livro> LoadActiveBooksWithoutStock() {
            return _context.Livros
                .Include(l => l.Estoque)
                .Where(l => l.IsAtivo && (l.Estoque == null || l.Estoque.Quantidade <= 0))
                .ToList();
        }

        public Dictionary<int, decimal> LoadSalesTotalsByBookIds(IReadOnlyCollection<int> livroIds) {
            if (livroIds.Count == 0) {
                return new Dictionary<int, decimal>();
            }

            return _context.PedidoItens
                .Include(item => item.Pedido)
                .Where(item =>
                    livroIds.Contains(item.LivroId) &&
                    item.Pedido != null &&
                    item.Pedido.Status != "REPROVADA" &&
                    item.Pedido.Status != "PAGAMENTO RECUSADO" &&
                    item.Pedido.Status != "CANCELADO")
                .GroupBy(item => item.LivroId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(item => item.PrecoUnitario * item.Quantidade));
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
