using Livros.Application.AdminBooks;
using Livros.Domain;

namespace Livros.Web.Models.ViewModels;

public sealed class AdminLivrosViewModel {
    public string Busca { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    public string Status { get; set; } = "ativos";
    public List<Categoria> CategoriasDisponiveis { get; set; } = new();
    public List<Livro> Livros { get; set; } = new();
}

public static class AdminLivrosViewModelMapper {
    public static AdminLivrosViewModel Map(AdminBooksCatalogResult result) {
        return new AdminLivrosViewModel {
            Busca = result.Busca,
            CategoriaId = result.CategoriaId,
            Status = result.Status,
            CategoriasDisponiveis = result.CategoriasDisponiveis,
            Livros = result.Livros
        };
    }
}
