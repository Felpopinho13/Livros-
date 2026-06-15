using Livros.Domain;

namespace Livros.Application.AdminInventory {
    public sealed class AdminInventoryService {
        private readonly IAdminInventoryDataProvider _dataProvider;

        public AdminInventoryService(IAdminInventoryDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public List<Estoque> ListActiveInventory() {
            EnsureStockRecords();
            return _dataProvider.LoadActiveInventoryWithBooks();
        }

        public AdminInventoryOperationResult AddStock(int livroId, int quantidade) {
            if (quantidade <= 0) {
                return new AdminInventoryOperationResult {
                    Succeeded = false,
                    Message = "Informe uma quantidade valida para adicionar ao estoque."
                };
            }

            var stock = _dataProvider.LoadStockByBookId(livroId);
            if (stock == null) {
                return new AdminInventoryOperationResult {
                    Succeeded = false,
                    Message = "Estoque nao encontrado para o livro informado."
                };
            }

            stock.Quantidade += quantidade;
            _dataProvider.SaveChanges();

            return new AdminInventoryOperationResult {
                Succeeded = true,
                Message = "Estoque atualizado com sucesso."
            };
        }

        public AdminInventoryOperationResult AdjustStock(int livroId, int novoValor) {
            if (novoValor < 0) {
                return new AdminInventoryOperationResult {
                    Succeeded = false,
                    Message = "O valor do estoque nao pode ser negativo."
                };
            }

            var stock = _dataProvider.LoadStockByBookId(livroId);
            if (stock == null) {
                return new AdminInventoryOperationResult {
                    Succeeded = false,
                    Message = "Estoque nao encontrado para o livro informado."
                };
            }

            stock.Quantidade = novoValor;
            _dataProvider.SaveChanges();

            return new AdminInventoryOperationResult {
                Succeeded = true,
                Message = "Estoque ajustado com sucesso."
            };
        }

        private void EnsureStockRecords() {
            var booksWithoutStock = _dataProvider.LoadActiveBooksWithoutStockRecord();
            if (!booksWithoutStock.Any()) {
                return;
            }

            var newStocks = booksWithoutStock.Select(book => new Estoque {
                LivroId = book.Id,
                Quantidade = 0
            });

            _dataProvider.AddStocks(newStocks);
            _dataProvider.SaveChanges();
        }
    }
}
