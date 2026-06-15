using Livros.Domain;

namespace Livros.Application.AdminBooks {
    public sealed class AdminBooksCatalogResult {
        public List<Categoria> CategoriasDisponiveis { get; init; } = new();
        public List<Livro> Livros { get; init; } = new();
    }

    public sealed class AdminBookCreateCommand {
        public Livro Livro { get; init; } = new();
        public IReadOnlyCollection<int> CategoriasIds { get; init; } = Array.Empty<int>();
    }

    public sealed class AdminBookCategoryUpdateCommand {
        public int LivroId { get; init; }
        public IReadOnlyCollection<int> CategoriasIds { get; init; } = Array.Empty<int>();
    }

    public sealed class AdminBookOperationResult {
        public bool Succeeded { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? LivroTitulo { get; init; }
    }
}
