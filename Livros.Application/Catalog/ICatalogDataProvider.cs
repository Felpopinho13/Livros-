using Livros.Domain;

namespace Livros.Application.Catalog {
    public interface ICatalogDataProvider {
        List<Livro> LoadActiveBooksWithStockAndCategories();
        Livro? LoadActiveBookByIdWithStockAndCategories(int id);
        List<Livro> LoadActiveBooksWithoutStock();
        Dictionary<int, decimal> LoadSalesTotalsByBookIds(IReadOnlyCollection<int> livroIds);
        void SaveChanges();
    }
}
