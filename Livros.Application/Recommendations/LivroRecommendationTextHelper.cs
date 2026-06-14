using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Livros.Domain;

namespace Livros.Application.Recommendations {
    public static class LivroRecommendationTextHelper {
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase) {
            "a", "as", "o", "os", "de", "da", "do", "das", "dos", "para", "por", "com", "sem",
            "um", "uma", "uns", "umas", "e", "ou", "que", "me", "tem", "tenho", "quero",
            "gostaria", "livro", "livros", "indique", "indicacao", "indicar", "recomende",
            "recomendar", "recomenda", "recomendacao", "sugerir", "sugestao", "sugestoes",
            "sobre", "interessante", "interessantes", "voce", "pode", "algum", "alguns",
            "alguma", "algumas", "quais", "sugere", "procuro", "procurando", "titulo",
            "titulos", "autor", "autora", "categoria", "categorias", "genero", "generos",
            "preco", "precos", "lista", "opcao", "opcoes", "ler", "leitura"
        };

        public static LivroRecommendationSearchIntent ExtractIntent(string message, IReadOnlyCollection<Livro> books) {
            return new LivroRecommendationSearchIntent {
                Author = TryExtractAuthorIntent(message, books),
                Category = TryExtractCategoryIntent(message, books),
                Title = TryExtractTitleIntent(message, books),
                WantsMoreOptions = LooksLikeMoreOptionsRequest(message)
            };
        }

        public static HashSet<string> Tokenize(string? text) {
            return (text ?? string.Empty)
                .Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}', '"' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeForMatch)
                .Where(token => token.Length >= 3 && !StopWords.Contains(token))
                .ToHashSet();
        }

        public static string NormalizeForMatch(string? text) {
            if (string.IsNullOrWhiteSpace(text)) {
                return string.Empty;
            }

            var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var character in normalized) {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) {
                    builder.Append(character);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static bool MatchesAuthor(string? authorName, string expectedAuthor) {
            var normalizedAuthor = NormalizeForMatch(authorName);
            var normalizedExpected = NormalizeForMatch(expectedAuthor);

            return !string.IsNullOrWhiteSpace(normalizedAuthor)
                && normalizedAuthor.Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesCategory(Livro book, string expectedCategory) {
            var normalizedExpected = NormalizeForMatch(expectedCategory);
            return (book.Categorias ?? new List<Categoria>())
                .Select(category => NormalizeForMatch(category.Nome))
                .Any(category => category.Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase));
        }

        public static bool MatchesTitle(string? title, string expectedTitle) {
            var normalizedTitle = NormalizeForMatch(title);
            var normalizedExpected = NormalizeForMatch(expectedTitle);

            return !string.IsNullOrWhiteSpace(normalizedTitle)
                && normalizedTitle.Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase);
        }

        public static bool BookMatchesToken(Livro book, string token) {
            if (string.IsNullOrWhiteSpace(token)) {
                return false;
            }

            var normalizedTitle = NormalizeForMatch(book.Titulo);
            var normalizedAuthor = NormalizeForMatch(book.Autor);
            var normalizedSynopsis = NormalizeForMatch(book.Sinopse);
            var normalizedPublisher = NormalizeForMatch(book.Editora);
            var normalizedCategories = (book.Categorias ?? new List<Categoria>())
                .Select(category => NormalizeForMatch(category.Nome));

            return normalizedTitle.Contains(token, StringComparison.OrdinalIgnoreCase)
                || normalizedAuthor.Contains(token, StringComparison.OrdinalIgnoreCase)
                || normalizedSynopsis.Contains(token, StringComparison.OrdinalIgnoreCase)
                || normalizedPublisher.Contains(token, StringComparison.OrdinalIgnoreCase)
                || normalizedCategories.Any(category => category.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        private static string? TryExtractAuthorIntent(string message, IReadOnlyCollection<Livro> books) {
            var patterns = new[] {
                @"escrit(?:o|os|a|as)\s+por\s+(?<author>.+)$",
                @"livros?\s+do\s+autor\s+(?<author>.+)$",
                @"livros?\s+da\s+autora\s+(?<author>.+)$",
                @"livros?\s+de\s+(?<author>.+)$",
                @"livros?\s+do\s+(?<author>.+)$",
                @"livros?\s+da\s+(?<author>.+)$",
                @"autor(?:a)?\s+(?<author>.+)$"
            };

            foreach (var pattern in patterns) {
                var match = Regex.Match(message.Trim(), pattern, RegexOptions.IgnoreCase);
                if (!match.Success) {
                    continue;
                }

                var candidate = SanitizeCatalogCandidate(match.Groups["author"].Value);
                var resolved = ResolveAuthorFromCatalog(candidate, books);
                if (!string.IsNullOrWhiteSpace(resolved)) {
                    return resolved;
                }
            }

            return ResolveAuthorFromCatalog(message, books);
        }

        private static string? TryExtractCategoryIntent(string message, IReadOnlyCollection<Livro> books) {
            var patterns = new[] {
                @"na\s+categoria\s+(?<category>.+)$",
                @"categoria\s+(?<category>.+)$",
                @"genero\s+(?<category>.+)$",
                @"livros?\s+de\s+(?<category>.+)$",
                @"algo\s+de\s+(?<category>.+)$"
            };

            foreach (var pattern in patterns) {
                var match = Regex.Match(message.Trim(), pattern, RegexOptions.IgnoreCase);
                if (!match.Success) {
                    continue;
                }

                var candidate = SanitizeCatalogCandidate(match.Groups["category"].Value);
                var resolved = ResolveCategoryFromCatalog(candidate, books);
                if (!string.IsNullOrWhiteSpace(resolved)) {
                    return resolved;
                }
            }

            return ResolveCategoryFromCatalog(message, books);
        }

        private static string? TryExtractTitleIntent(string message, IReadOnlyCollection<Livro> books) {
            var normalizedMessage = NormalizeForMatch(message);

            return books
                .Select(book => book.Titulo)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(title => new {
                    Original = title!,
                    Normalized = NormalizeForMatch(title)
                })
                .Where(title => normalizedMessage.Contains(title.Normalized, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(title => title.Normalized.Length)
                .Select(title => title.Original)
                .FirstOrDefault();
        }

        private static string? ResolveAuthorFromCatalog(string candidateText, IReadOnlyCollection<Livro> books) {
            var normalizedCandidate = NormalizeForMatch(candidateText);
            if (string.IsNullOrWhiteSpace(normalizedCandidate)) {
                return null;
            }

            var tokens = Tokenize(candidateText);
            if (!tokens.Any()) {
                return null;
            }

            var authors = books
                .Select(book => book.Autor)
                .Where(author => !string.IsNullOrWhiteSpace(author))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(author => new {
                    Original = author!,
                    Normalized = NormalizeForMatch(author)
                })
                .ToList();

            var exactMatch = authors.FirstOrDefault(author =>
                string.Equals(author.Normalized, normalizedCandidate, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) {
                return exactMatch.Original;
            }

            var phraseMatch = authors.FirstOrDefault(author =>
                normalizedCandidate.Contains(author.Normalized, StringComparison.OrdinalIgnoreCase)
                || author.Normalized.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase));
            if (phraseMatch != null) {
                return phraseMatch.Original;
            }

            return authors
                .Where(author => tokens.All(token => author.Normalized.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(author => author.Normalized.Length)
                .Select(author => author.Original)
                .FirstOrDefault();
        }

        private static string? ResolveCategoryFromCatalog(string candidateText, IReadOnlyCollection<Livro> books) {
            var normalizedCandidate = NormalizeForMatch(candidateText);
            if (string.IsNullOrWhiteSpace(normalizedCandidate)) {
                return null;
            }

            var tokens = Tokenize(candidateText);
            var categories = books
                .SelectMany(book => book.Categorias ?? new List<Categoria>())
                .Select(category => category.Nome)
                .Concat(CategoriaCatalogo.Itens.Select(item => item.Nome))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new {
                    Original = name,
                    Normalized = NormalizeForMatch(name)
                })
                .ToList();

            var exactMatch = categories.FirstOrDefault(category =>
                string.Equals(category.Normalized, normalizedCandidate, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) {
                return exactMatch.Original;
            }

            var phraseMatch = categories
                .Where(category =>
                    normalizedCandidate.Contains(category.Normalized, StringComparison.OrdinalIgnoreCase)
                    || category.Normalized.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(category => category.Normalized.Length)
                .FirstOrDefault();
            if (phraseMatch != null) {
                return phraseMatch.Original;
            }

            if (!tokens.Any()) {
                return null;
            }

            return categories
                .Where(category => tokens.All(token => category.Normalized.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(category => category.Normalized.Length)
                .Select(category => category.Original)
                .FirstOrDefault();
        }

        private static string SanitizeCatalogCandidate(string? text) {
            if (string.IsNullOrWhiteSpace(text)) {
                return string.Empty;
            }

            return text.Trim(' ', '.', '!', '?', '"');
        }

        private static bool LooksLikeMoreOptionsRequest(string message) {
            var normalized = NormalizeForMatch(message);
            return normalized.Contains("mais", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("outras", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("outros", StringComparison.OrdinalIgnoreCase);
        }
    }
}
