using Livros.Domain;

namespace Livros.Application.Catalog {
    public sealed class CatalogListQuery {
        public string? Busca { get; init; }
    }

    public sealed class CatalogListResult {
        public string Busca { get; init; } = string.Empty;
        public List<Livro> Livros { get; init; } = new();
    }
}
