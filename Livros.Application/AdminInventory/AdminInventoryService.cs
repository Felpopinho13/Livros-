using Livros.Application.Common.Logging;
using Livros.Domain;

namespace Livros.Application.AdminInventory {
    public sealed class AdminInventoryService {
        private readonly IAdminInventoryDataProvider _dataProvider;
        private readonly IAppLogger<AdminInventoryService> _logger;

        public AdminInventoryService(IAdminInventoryDataProvider dataProvider, IAppLogger<AdminInventoryService> logger) {
            _dataProvider = dataProvider;
            _logger = logger;
        }

        public List<Estoque> ListActiveInventory() {
            EnsureStockRecords();
            return _dataProvider.LoadActiveInventoryWithBooks();
        }

        public AdminInventoryOperationResult AddStock(int livroId, int quantidade) {
            if (quantidade <= 0) {
                _logger.LogWarning("Tentativa de adicionar estoque com quantidade invalida. LivroId: {LivroId}, Quantidade: {Quantidade}", livroId, quantidade);
                return new AdminInventoryOperationResult {
                    Succeeded = false,
                    Message = "Informe uma quantidade valida para adicionar ao estoque."
                };
            }

            var stock = _dataProvider.LoadStockByBookId(livroId);
            if (stock == null) {
                _logger.LogWarning("Estoque nao encontrado ao adicionar saldo. LivroId: {LivroId}", livroId);
                return new AdminInventoryOperationResult {
                    Succeeded = false,
                    Message = "Estoque nao encontrado para o livro informado."
                };
            }

            stock.Quantidade += quantidade;
            _dataProvider.SaveChanges();
            _logger.LogInformation("Estoque adicionado. LivroId: {LivroId}, QuantidadeAdicionada: {Quantidade}, NovoSaldo: {NovoSaldo}", livroId, quantidade, stock.Quantidade);

            return new AdminInventoryOperationResult {
                Succeeded = true,
                Message = "Estoque atualizado com sucesso."
            };
        }

        public AdminInventoryOperationResult AdjustStock(int livroId, int novoValor) {
            if (novoValor < 0) {
                _logger.LogWarning("Tentativa de ajustar estoque com valor negativo. LivroId: {LivroId}, NovoValor: {NovoValor}", livroId, novoValor);
                return new AdminInventoryOperationResult {
                    Succeeded = false,
                    Message = "O valor do estoque nao pode ser negativo."
                };
            }

            var stock = _dataProvider.LoadStockByBookId(livroId);
            if (stock == null) {
                _logger.LogWarning("Estoque nao encontrado ao ajustar saldo. LivroId: {LivroId}", livroId);
                return new AdminInventoryOperationResult {
                    Succeeded = false,
                    Message = "Estoque nao encontrado para o livro informado."
                };
            }

            stock.Quantidade = novoValor;
            _dataProvider.SaveChanges();
            _logger.LogInformation("Estoque ajustado. LivroId: {LivroId}, NovoSaldo: {NovoSaldo}", livroId, stock.Quantidade);

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
            _logger.LogInformation("Registros de estoque criados automaticamente para {QuantidadeLivros} livro(s) sem estoque.", booksWithoutStock.Count);
        }
    }
}
