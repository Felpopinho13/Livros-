using Livros.Domain;

namespace Livros.Web.Services {
    internal sealed class LivroRecommendationCustomerProfile {
        public static LivroRecommendationCustomerProfile Empty { get; } = new();

        public bool HasHistory => PurchasedBookIds.Count > 0;
        public HashSet<int> PurchasedBookIds { get; init; } = new();
        public Dictionary<string, int> CategoryWeights { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> AuthorWeights { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class LivroRecommendationSearchIntent {
        public string? Author { get; init; }
        public string? Category { get; init; }
        public string? Title { get; init; }
        public bool WantsMoreOptions { get; init; }

        public bool HasCatalogConstraint =>
            !string.IsNullOrWhiteSpace(Author)
            || !string.IsNullOrWhiteSpace(Category)
            || !string.IsNullOrWhiteSpace(Title);
    }

    internal sealed class LivroRecommendationScoredBook {
        public required Livro Book { get; init; }
        public int DirectMatchScore { get; init; }
        public int Score { get; init; }
    }
}
