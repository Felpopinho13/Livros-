using System.Text;
using Livros.Domain;

namespace Livros.Application.Recommendations {
    public static class LivroRecommendationEngine {
        public static List<Livro> FilterCandidateBooks(IEnumerable<Livro> books, LivroRecommendationSearchIntent intent) {
            var filtered = books;

            if (!string.IsNullOrWhiteSpace(intent.Author)) {
                filtered = filtered.Where(book => LivroRecommendationTextHelper.MatchesAuthor(book.Autor, intent.Author));
            }

            if (!string.IsNullOrWhiteSpace(intent.Category)) {
                filtered = filtered.Where(book => LivroRecommendationTextHelper.MatchesCategory(book, intent.Category));
            }

            if (!string.IsNullOrWhiteSpace(intent.Title)) {
                filtered = filtered.Where(book => LivroRecommendationTextHelper.MatchesTitle(book.Titulo, intent.Title));
            }

            return filtered.ToList();
        }

        public static List<LivroRecommendationScoredBook> ScoreBooks(
            IReadOnlyCollection<Livro> books,
            string message,
            LivroRecommendationCustomerProfile customerProfile,
            IReadOnlyDictionary<int, int> popularityByBookId,
            IReadOnlyCollection<int> lastRecommendedBookIds) {
            var tokens = LivroRecommendationTextHelper.Tokenize(message);
            var lastRecommended = lastRecommendedBookIds.ToHashSet();

            return books
                .Select(book => {
                    var directMatchScore = CalculateDirectMatchScore(book, tokens);
                    var score = directMatchScore;

                    if (!string.IsNullOrWhiteSpace(book.Autor) && customerProfile.AuthorWeights.TryGetValue(book.Autor, out var authorWeight)) {
                        score += authorWeight * 10;
                    }

                    foreach (var category in book.Categorias ?? new List<Categoria>()) {
                        if (customerProfile.CategoryWeights.TryGetValue(category.Nome, out var categoryWeight)) {
                            score += categoryWeight * 8;
                        }
                    }

                    if (popularityByBookId.TryGetValue(book.Id, out var popularity)) {
                        score += Math.Min(popularity * 3, 45);
                    }

                    if (customerProfile.PurchasedBookIds.Contains(book.Id)) {
                        score -= 120;
                    }

                    if (lastRecommended.Contains(book.Id)) {
                        score -= 30;
                    }

                    return new LivroRecommendationScoredBook {
                        Book = book,
                        DirectMatchScore = directMatchScore,
                        Score = score
                    };
                })
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.DirectMatchScore)
                .ThenBy(item => item.Book.Titulo)
                .ToList();
        }

        public static List<LivroRecommendationScoredBook> SelectCandidates(
            IReadOnlyList<LivroRecommendationScoredBook> scoredBooks,
            LivroRecommendationSearchIntent intent) {
            var candidates = scoredBooks;

            if (intent.WantsMoreOptions && scoredBooks.Count > 3) {
                candidates = scoredBooks
                    .Where(book => book.Score > 0)
                    .Skip(1)
                    .ToList();
            }

            if (!intent.HasCatalogConstraint && !candidates.Any(book => book.DirectMatchScore > 0)) {
                return candidates.Take(3).ToList();
            }

            var directMatches = candidates
                .Where(book => !intent.HasCatalogConstraint || book.DirectMatchScore > 0 || book.Score > 0)
                .Take(3)
                .ToList();

            return directMatches.Any()
                ? directMatches
                : candidates.Take(3).ToList();
        }

        public static bool IsFallbackRecommendationSafe(
            string message,
            IReadOnlyList<LivroRecommendationScoredBook> selectedBooks,
            LivroRecommendationSearchIntent intent) {
            if (!selectedBooks.Any()) {
                return false;
            }

            var tokens = LivroRecommendationTextHelper.Tokenize(message).ToList();
            if (!tokens.Any()) {
                return true;
            }

            var matchedTokens = tokens.Count(token =>
                selectedBooks.Any(item => LivroRecommendationTextHelper.BookMatchesToken(item.Book, token)));

            if (matchedTokens == 0) {
                return false;
            }

            var coverage = (decimal)matchedTokens / tokens.Count;
            if (coverage >= 0.6m) {
                return true;
            }

            return !intent.HasCatalogConstraint && matchedTokens == tokens.Count;
        }

        public static List<LivroRecommendationSuggestion> BuildSuggestions(
            IReadOnlyList<LivroRecommendationScoredBook> selectedBooks,
            string message,
            LivroRecommendationCustomerProfile customerProfile,
            IReadOnlyDictionary<int, int> popularityByBookId) {
            return selectedBooks
                .Take(3)
                .Select(item => new LivroRecommendationSuggestion {
                    Book = item.Book,
                    Reason = BuildReason(item.Book, message, customerProfile, popularityByBookId),
                    Categories = (item.Book.Categorias ?? new List<Categoria>())
                        .OrderBy(category => category.Nome)
                        .Select(category => category.Nome)
                        .ToList()
                })
                .ToList();
        }

        public static string BuildFallbackReply(
            string message,
            IReadOnlyList<LivroRecommendationSuggestion> suggestions,
            LivroRecommendationSearchIntent intent,
            LivroRecommendationCustomerProfile customerProfile) {
            var builder = new StringBuilder();
            builder.Append($"Encontrei {suggestions.Count} livro(s) do catalogo relacionados aos termos da sua busca");

            if (!string.IsNullOrWhiteSpace(message)) {
                builder.Append($" sobre \"{message}\"");
            }

            builder.Append('.');

            if (customerProfile.HasHistory) {
                builder.Append(" Tambem considerei o historico de compras do cliente para priorizar os titulos.");
            }

            builder.Append(" Minhas principais sugestoes sao: ");
            builder.Append(string.Join(", ", suggestions.Select(item => item.Book.Titulo)));
            builder.Append('.');

            if (intent.WantsMoreOptions) {
                builder.Append(" Se quiser, eu posso buscar outras opcoes sem repetir os mesmos livros.");
            }
            else {
                builder.Append(" Se quiser, eu posso refinar por categoria, autor ou faixa de preco.");
            }

            return builder.ToString();
        }

        public static string BuildNoResultReply(LivroRecommendationSearchIntent intent) {
            if (!string.IsNullOrWhiteSpace(intent.Author) && !string.IsNullOrWhiteSpace(intent.Category)) {
                return $"Nao encontrei livros ativos do catalogo escritos por {intent.Author} na categoria {intent.Category}.";
            }

            if (!string.IsNullOrWhiteSpace(intent.Author)) {
                return $"Nao encontrei livros ativos do catalogo escritos por {intent.Author}.";
            }

            if (!string.IsNullOrWhiteSpace(intent.Category)) {
                return $"Nao encontrei livros ativos do catalogo na categoria {intent.Category}.";
            }

            if (!string.IsNullOrWhiteSpace(intent.Title)) {
                return $"Nao encontrei um livro ativo do catalogo com o titulo {intent.Title}.";
            }

            return "Nao encontrei livros do catalogo para esse pedido especifico. Se quiser, tente informar um titulo, tema, autor ou categoria.";
        }

        public static string BuildLowConfidenceReply(LivroRecommendationSearchIntent intent) {
            if (intent.HasCatalogConstraint) {
                return "Encontrei referencias para parte do seu pedido, mas nao ha base suficiente no catalogo para recomendar livros para esse objetivo de forma confiavel. Posso ajudar melhor por titulo, autor, categoria ou tema de leitura.";
            }

            return "Desculpe, nao consegui relacionar esse pedido aos livros do catalogo com confianca. Posso ajudar com titulo, autor, categoria, preco ou tema de leitura.";
        }

        private static int CalculateDirectMatchScore(Livro book, IReadOnlyCollection<string> tokens) {
            if (!tokens.Any()) {
                return 10;
            }

            var normalizedTitle = LivroRecommendationTextHelper.NormalizeForMatch(book.Titulo);
            var normalizedAuthor = LivroRecommendationTextHelper.NormalizeForMatch(book.Autor);
            var normalizedSynopsis = LivroRecommendationTextHelper.NormalizeForMatch(book.Sinopse);
            var normalizedPublisher = LivroRecommendationTextHelper.NormalizeForMatch(book.Editora);
            var normalizedCategories = (book.Categorias ?? new List<Categoria>())
                .Select(category => LivroRecommendationTextHelper.NormalizeForMatch(category.Nome))
                .ToList();

            var score = 0;

            foreach (var token in tokens) {
                if (normalizedTitle.Contains(token, StringComparison.OrdinalIgnoreCase)) {
                    score += 120;
                }

                if (normalizedAuthor.Contains(token, StringComparison.OrdinalIgnoreCase)) {
                    score += 90;
                }

                if (normalizedCategories.Any(category => category.Contains(token, StringComparison.OrdinalIgnoreCase))) {
                    score += 70;
                }

                if (normalizedSynopsis.Contains(token, StringComparison.OrdinalIgnoreCase)) {
                    score += 45;
                }

                if (normalizedPublisher.Contains(token, StringComparison.OrdinalIgnoreCase)) {
                    score += 20;
                }
            }

            return score;
        }

        private static string BuildReason(
            Livro book,
            string message,
            LivroRecommendationCustomerProfile customerProfile,
            IReadOnlyDictionary<int, int> popularityByBookId) {
            var normalizedMessage = LivroRecommendationTextHelper.NormalizeForMatch(message);
            var normalizedTitle = LivroRecommendationTextHelper.NormalizeForMatch(book.Titulo);
            var normalizedAuthor = LivroRecommendationTextHelper.NormalizeForMatch(book.Autor);
            var normalizedSynopsis = LivroRecommendationTextHelper.NormalizeForMatch(book.Sinopse);
            var categories = (book.Categorias ?? new List<Categoria>())
                .Select(category => category.Nome)
                .ToList();

            if (!string.IsNullOrWhiteSpace(message) && normalizedMessage.Contains(normalizedTitle, StringComparison.OrdinalIgnoreCase)) {
                return "Combina diretamente com o titulo pedido.";
            }

            if (!string.IsNullOrWhiteSpace(message) && normalizedMessage.Contains(normalizedAuthor, StringComparison.OrdinalIgnoreCase)) {
                return "Atende ao autor pedido na busca.";
            }

            var matchingCategory = categories.FirstOrDefault(category =>
                normalizedMessage.Contains(LivroRecommendationTextHelper.NormalizeForMatch(category), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(matchingCategory)) {
                return $"Combina com a categoria {matchingCategory}.";
            }

            if (!string.IsNullOrWhiteSpace(message) && LivroRecommendationTextHelper.Tokenize(message).Any(token => normalizedSynopsis.Contains(token, StringComparison.OrdinalIgnoreCase))) {
                return "Tem relacao com os termos informados na busca.";
            }

            if (!string.IsNullOrWhiteSpace(book.Autor) && customerProfile.AuthorWeights.ContainsKey(book.Autor)) {
                return "Segue um autor que aparece no historico do cliente.";
            }

            if (categories.Any(category => customerProfile.CategoryWeights.ContainsKey(category))) {
                return "A categoria combina com compras anteriores do cliente.";
            }

            if (popularityByBookId.TryGetValue(book.Id, out var popularity) && popularity > 0) {
                return "Esta entre os livros com melhor saida no catalogo.";
            }

            return "Foi selecionado entre os livros ativos do catalogo.";
        }
    }
}
