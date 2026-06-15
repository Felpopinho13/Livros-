using Livros.Domain;

namespace Livros.Application.AdminInventory {
    public interface IAdminInventoryDataProvider {
        List<Livro> LoadActiveBooksWithoutStockRecord();
        void AddStocks(IEnumerable<Estoque> stocks);
        List<Estoque> LoadActiveInventoryWithBooks();
        Estoque? LoadStockByBookId(int livroId);
        void SaveChanges();
    }
}
