using Livros.Application.Catalog;
using Livros.Domain;

namespace Livros.Web.Models.ViewModels;

public sealed class CatalogoViewModel {
    public string Busca { get; set; } = string.Empty;
    public List<Livro> Livros { get; set; } = new();
    public bool PossuiBusca => !string.IsNullOrWhiteSpace(Busca);
}

public static class CatalogoViewModelMapper {
    public static CatalogoViewModel Map(CatalogListResult result) {
        return new CatalogoViewModel {
            Busca = result.Busca,
            Livros = result.Livros
        };
    }
}
