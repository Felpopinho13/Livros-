using Livros.Domain;

namespace Livros.Application.AdminBooks {
    public interface IAdminBooksDataProvider {
        List<Categoria> LoadCategories();
        List<Categoria> LoadCategoriesByIds(IReadOnlyCollection<int> categoryIds);
        List<Livro> LoadBooksWithStockAndCategories(AdminBooksCatalogQuery query);
        Livro? LoadBookByIdWithCategories(int livroId);
        void AddBook(Livro livro);
        void AddStock(Estoque estoque);
        void SaveChanges();
    }
}
