using Livros.Domain;

namespace Livros.Application.Catalog {
    public sealed class CatalogService {
        private const decimal MinimumSalesToKeepBookActiveWithoutStock = 50m;
        private readonly ICatalogDataProvider _dataProvider;

        public CatalogService(ICatalogDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public CatalogListResult ListActiveBooks(CatalogListQuery? query = null) {
            ApplyAutomaticInactivationWithoutStock();

            var normalizedQuery = new CatalogListQuery {
                Busca = query?.Busca?.Trim()
            };

            return new CatalogListResult {
                Busca = normalizedQuery.Busca ?? string.Empty,
                Livros = _dataProvider.LoadActiveBooksWithStockAndCategories(normalizedQuery)
            };
        }

        public Livro? GetBookDetails(int id) {
            ApplyAutomaticInactivationWithoutStock();
            return _dataProvider.LoadActiveBookByIdWithStockAndCategories(id);
        }

        private void ApplyAutomaticInactivationWithoutStock() {
            var booksWithoutStock = _dataProvider.LoadActiveBooksWithoutStock();
            if (!booksWithoutStock.Any()) {
                return;
            }

            var bookIds = booksWithoutStock.Select(book => book.Id).ToList();
            var salesTotals = _dataProvider.LoadSalesTotalsByBookIds(bookIds);
            var changed = false;

            foreach (var book in booksWithoutStock) {
                var soldValue = salesTotals.TryGetValue(book.Id, out var totalSold)
                    ? totalSold
                    : 0;

                if (soldValue < MinimumSalesToKeepBookActiveWithoutStock) {
                    book.IsAtivo = false;
                    changed = true;
                }
            }

            if (changed) {
                _dataProvider.SaveChanges();
            }
        }
    }
}
