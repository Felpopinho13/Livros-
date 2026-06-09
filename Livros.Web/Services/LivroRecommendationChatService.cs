using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Livros.Web.Configuration;
using Livros.Web.Models.Chatbot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Livros.Web.Services {
    public sealed class LivroRecommendationChatService {
        private static readonly HashSet<string> InvalidOrderStatuses = new(StringComparer.OrdinalIgnoreCase) {
            "REPROVADA",
            "PAGAMENTO RECUSADO",
            "CANCELADO"
        };

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase) {
            "a", "as", "o", "os", "de", "da", "do", "das", "dos", "para", "por", "com", "sem",
            "um", "uma", "uns", "umas", "e", "ou", "que", "me", "tem", "tenho", "quero",
            "gostaria", "livro", "livros", "indique", "indicacao", "indicar", "recomende",
            "recomendar", "recomenda", "recomendacao", "sugerir",
            "sobre", "aprender", "estudar", "ler", "interessante", "interessantes", "voce",
            "pode", "algum", "alguns", "alguma", "algumas", "quais", "sugere", "sugestao",
            "sugestoes", "procuro", "procurando"
        };

        private static readonly string[] ContextTriggerKeywords = {
            "durante",
            "enquanto",
            "assistindo",
            "vendo",
            "para ler",
            "ler no",
            "ler na",
            "ler num",
            "ler numa",
            "ler em",
            "ler durante"
        };

        private static readonly string[] LearningTriggerKeywords = {
            "ensine",
            "ensinar",
            "ensina",
            "aprender",
            "aprenda",
            "como",
            "curso",
            "treinar",
            "treino"
        };

        private static readonly HashSet<string> NarrativeCategoryKeywords = new(StringComparer.OrdinalIgnoreCase) {
            "aventura",
            "classicos",
            "contos",
            "drama",
            "fantasia",
            "ficcao",
            "ficcao cientifica",
            "infantil",
            "juvenil",
            "misterio",
            "romance",
            "suspense",
            "terror",
            "hqs e mangas"
        };

        private static readonly ContextFamily[] IncompatibleReadingContexts = {
            new(
                "atividade fisica ou evento esportivo",
                "Ler nesse momento talvez nao seja muito pratico.",
                "Se a ideia for escolher uma leitura para antes ou depois, eu posso te sugerir estes livros do catalogo: ",
                new[] {
                    "champions",
                    "champions league",
                    "futebol",
                    "volei",
                    "vôlei",
                    "basquete",
                    "corrida",
                    "maratona",
                    "academia",
                    "treino",
                    "ciclismo",
                    "pedal",
                    "pedalando",
                    "natação",
                    "natacao",
                    "jogo",
                    "partida",
                    "estadio",
                    "torcida"
                }),
            new(
                "deslocamento ou situacao de risco",
                "Ler nessa situacao pode tirar a atencao do que realmente importa.",
                "Se quiser escolher um livro para outro momento mais tranquilo, eu posso te sugerir estes titulos do catalogo: ",
                new[] {
                    "dirigindo",
                    "dirigir",
                    "volante",
                    "moto",
                    "pilotando",
                    "bicicleta",
                    "atravessando",
                    "trânsito",
                    "transito",
                    "estrada"
                }),
            new(
                "situacao solene",
                "Esse nao parece um momento apropriado para leitura.",
                "Se a intencao for separar um livro para antes ou depois, eu posso te sugerir estes titulos do catalogo: ",
                new[] {
                    "enterro",
                    "velorio",
                    "velório",
                    "funeral",
                    "missa",
                    "casamento",
                    "cerimonia",
                    "cerimônia"
                }),
            new(
                "situacao inviavel para leitura",
                "Nesse contexto a leitura nao parece viavel.",
                "Se a ideia for guardar uma leitura para um momento melhor, eu posso te sugerir estes livros do catalogo: ",
                new[] {
                    "dormindo",
                    "sono",
                    "banho",
                    "chuveiro",
                    "debaixo dagua",
                    "debaixo d'agua",
                    "nadando"
                })
        };

        private static readonly PurposeMismatchFamily[] IncompatibleLearningPurposes = {
            new(
                "nadar",
                "Um livro de ficcao nao seria a melhor forma de aprender a nadar.",
                "Se a ideia for manter um clima de ficcao ou narrativa, eu posso te sugerir estes livros do catalogo: ",
                false,
                new[] {
                    "nadar",
                    "nadando",
                    "natacao",
                    "piscina",
                    "mergulho",
                    "surfe"
                }),
            new(
                "dirigir ou pilotar",
                "Um livro de ficcao nao seria a melhor forma de aprender a dirigir ou pilotar com seguranca.",
                "Se quiser manter o genero pedido para outro momento, eu posso te sugerir estes livros do catalogo: ",
                false,
                new[] {
                    "dirigir",
                    "dirigindo",
                    "volante",
                    "pilotar",
                    "moto",
                    "caminhao",
                    "aviao",
                    "voar"
                }),
            new(
                "cirurgia ou procedimento tecnico",
                "Esse tipo de aprendizado exige material tecnico especifico, nao um livro de ficcao ou narrativa.",
                "Se a ideia for escolher uma leitura do catalogo para outro objetivo, eu posso te sugerir estes livros: ",
                false,
                new[] {
                    "cirurgia",
                    "operar",
                    "suturar",
                    "medicina",
                    "procedimento"
                }),
            new(
                "conserto ou manutencao pratica",
                "Um livro de ficcao nao seria a melhor referencia para conserto ou manutencao pratica.",
                "Se quiser, eu posso te redirecionar para livros do catalogo que combinem melhor com o seu gosto de leitura: ",
                false,
                new[] {
                    "consertar",
                    "geladeira",
                    "encanamento",
                    "eletrica",
                    "eletrico",
                    "manutencao",
                    "reparo"
                }),
            new(
                "resultado irreal ou fora do escopo",
                "Esse pedido foge do que um livro do catalogo realmente pode entregar.",
                "Se quiser, eu posso sugerir livros do catalogo que combinem com o genero ou autor que voce pediu: ",
                true,
                new[] {
                    "loteria",
                    "ganhar na loteria",
                    "voar",
                    "teletransporte",
                    "curar tudo",
                    "resolver depressao em 1 dia"
                })
        };

        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly OpenAiOptions _openAiOptions;
        private readonly ILogger<LivroRecommendationChatService> _logger;

        public LivroRecommendationChatService(
            AppDbContext context,
            HttpClient httpClient,
            IOptions<OpenAiOptions> openAiOptions,
            ILogger<LivroRecommendationChatService> logger) {
            _context = context;
            _httpClient = httpClient;
            _openAiOptions = openAiOptions.Value;
            _logger = logger;
        }

        public async Task<ChatbotResponse> RecommendAsync(
            string message,
            int? clienteId,
            ChatbotSessionState sessionState,
            CancellationToken cancellationToken = default) {
            var customerProfile = await BuildCustomerProfileAsync(clienteId, cancellationToken);
            var popularityByBookId = await GetPopularityByBookIdAsync(cancellationToken);
            var books = await _context.Livros
                .AsNoTracking()
                .Include(l => l.Categorias)
                .Include(l => l.Estoque)
                .Where(l => l.IsAtivo)
                .ToListAsync(cancellationToken);
            var authorIntent = TryExtractAuthorIntent(message, books);
            var categoryIntent = TryExtractCategoryIntent(message, books);
            var candidateBooks = FilterBooksByIntent(books, authorIntent, categoryIntent);

            if (authorIntent != null && categoryIntent != null && !candidateBooks.Any()) {
                var combinedReply = $"Nao encontrei livros ativos do catalogo escritos por {authorIntent.DisplayText} na categoria {categoryIntent.DisplayText}.";
                UpdateSessionState(sessionState, message, combinedReply, Array.Empty<int>());

                return new ChatbotResponse {
                    Reply = combinedReply,
                    UsedAi = false,
                    Source = "fallback"
                };
            }

            if (authorIntent != null && !candidateBooks.Any()) {
                var authorReply = $"Nao encontrei livros do catalogo escritos por {authorIntent.DisplayText}.";
                UpdateSessionState(sessionState, message, authorReply, Array.Empty<int>());

                return new ChatbotResponse {
                    Reply = authorReply,
                    UsedAi = false,
                    Source = "fallback"
                };
            }

            if (categoryIntent != null && !candidateBooks.Any()) {
                var categoryReply = $"Nao encontrei livros ativos do catalogo na categoria {categoryIntent.DisplayText}.";
                UpdateSessionState(sessionState, message, categoryReply, Array.Empty<int>());

                return new ChatbotResponse {
                    Reply = categoryReply,
                    UsedAi = false,
                    Source = "fallback"
                };
            }

            var scoredBooks = ScoreBooks(candidateBooks, message, customerProfile, popularityByBookId, sessionState);
            var hasSpecificIntent = Tokenize(message).Any();
            var genericBooks = scoredBooks
                .Where(x => x.Score > 0)
                .Take(3)
                .ToList();

            var activityContextReply = TryBuildActivityContextReply(
                message,
                genericBooks,
                customerProfile,
                popularityByBookId);

            if (activityContextReply != null) {
                UpdateSessionState(sessionState, message, activityContextReply.Reply, activityContextReply.Recommendations.Select(x => x.Id));
                return activityContextReply;
            }

            var purposeMismatchReply = TryBuildPurposeMismatchReply(
                message,
                authorIntent,
                categoryIntent,
                genericBooks,
                customerProfile,
                popularityByBookId);

            if (purposeMismatchReply != null) {
                UpdateSessionState(sessionState, message, purposeMismatchReply.Reply, purposeMismatchReply.Recommendations.Select(x => x.Id));
                return purposeMismatchReply;
            }

            if (hasSpecificIntent && scoredBooks.All(x => x.DirectMatchScore <= 0)) {
                var emptyReply = "Nao encontrei livros do catalogo que atendam a esse pedido especifico. Se quiser, tente informar um tema, linguagem, autor ou categoria.";
                UpdateSessionState(sessionState, message, emptyReply, Array.Empty<int>());

                return new ChatbotResponse {
                    Reply = emptyReply,
                    UsedAi = false,
                    Source = "fallback"
                };
            }

            var selectedBooks = scoredBooks
                .Where(x => x.Score > 0)
                .Take(3)
                .ToList();

            if (!selectedBooks.Any()) {
                var emptyReply = "Nao encontrei livros do catalogo que combinem com esse pedido. Tente citar um tema, autor, categoria ou objetivo de leitura.";
                UpdateSessionState(sessionState, message, emptyReply, Array.Empty<int>());

                return new ChatbotResponse {
                    Reply = emptyReply,
                    UsedAi = false,
                    Source = "fallback"
                };
            }

            var recommendedBooks = selectedBooks
                .Select(x => new RecommendedBookDto {
                    Id = x.Book.Id,
                    Title = x.Book.Titulo,
                    Author = x.Book.Autor,
                    Price = $"R$ {x.Book.Preco:N2}",
                    ImageUrl = x.Book.ImagemUrl,
                    DetailsUrl = $"/Home/Detalhes/{x.Book.Id}",
                    Reason = BuildReason(x.Book, message, customerProfile, popularityByBookId),
                    Categories = x.Book.Categorias?
                        .OrderBy(c => c.Nome)
                        .Select(c => c.Nome)
                        .ToList() ?? new List<string>()
                })
                .ToList();

            var reply = BuildFallbackReply(message, recommendedBooks, customerProfile);
            var usedAi = false;
            var source = "fallback";

            if (CanUseOpenAi()) {
                try {
                    var aiReply = await GenerateAiReplyAsync(message, recommendedBooks, customerProfile, sessionState, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(aiReply)) {
                        reply = aiReply.Trim();
                        usedAi = true;
                        source = "openai";
                    }
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Falha ao gerar resposta do chatbot com OpenAI. Usando fallback local.");
                }
            }

            UpdateSessionState(sessionState, message, reply, recommendedBooks.Select(x => x.Id));

            return new ChatbotResponse {
                Reply = reply,
                UsedAi = usedAi,
                Source = source,
                Recommendations = recommendedBooks
            };
        }

        private bool CanUseOpenAi() {
            return !string.IsNullOrWhiteSpace(_openAiOptions.ApiKey);
        }

        private async Task<CustomerProfile> BuildCustomerProfileAsync(int? clienteId, CancellationToken cancellationToken) {
            if (!clienteId.HasValue) {
                return CustomerProfile.Empty;
            }

            var purchaseItems = await _context.PedidoItens
                .AsNoTracking()
                .Include(i => i.Pedido)
                .Include(i => i.Livro)
                    .ThenInclude(l => l.Categorias)
                .Where(i => i.Pedido.ClienteId == clienteId.Value && !InvalidOrderStatuses.Contains(i.Pedido.Status))
                .ToListAsync(cancellationToken);

            var purchasedBookIds = purchaseItems
                .Select(i => i.LivroId)
                .Distinct()
                .ToHashSet();

            var categoryWeights = purchaseItems
                .SelectMany(i => (i.Livro.Categorias ?? new List<Categoria>())
                    .Select(c => new { c.Nome, Weight = i.Quantidade }))
                .GroupBy(x => x.Nome)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Weight), StringComparer.OrdinalIgnoreCase);

            var authorWeights = purchaseItems
                .GroupBy(i => i.Livro.Autor)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade), StringComparer.OrdinalIgnoreCase);

            return new CustomerProfile {
                PurchasedBookIds = purchasedBookIds,
                CategoryWeights = categoryWeights,
                AuthorWeights = authorWeights
            };
        }

        private async Task<Dictionary<int, int>> GetPopularityByBookIdAsync(CancellationToken cancellationToken) {
            return await _context.PedidoItens
                .AsNoTracking()
                .Include(i => i.Pedido)
                .Where(i => !InvalidOrderStatuses.Contains(i.Pedido.Status))
                .GroupBy(i => i.LivroId)
                .Select(g => new { LivroId = g.Key, Quantity = g.Sum(x => x.Quantidade) })
                .ToDictionaryAsync(x => x.LivroId, x => x.Quantity, cancellationToken);
        }

        private List<ScoredBook> ScoreBooks(
            List<Livro> books,
            string message,
            CustomerProfile customerProfile,
            IReadOnlyDictionary<int, int> popularityByBookId,
            ChatbotSessionState sessionState) {
            var tokens = Tokenize(message);
            var hasMeaningfulTokens = tokens.Any();
            var lastRecommendations = sessionState.LastRecommendedBookIds.ToHashSet();

            return books
                .Where(l => l.Estoque != null && l.Estoque.Quantidade > 0)
                .Select(book => new ScoredBook {
                    Book = book,
                    DirectMatchScore = CalculateDirectMatchScore(book, tokens),
                    Score = CalculateScore(book, tokens, hasMeaningfulTokens, customerProfile, popularityByBookId, lastRecommendations)
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Book.Titulo)
                .ToList();
        }

        private List<Livro> FilterBooksByIntent(
            List<Livro> books,
            AuthorIntent? authorIntent,
            CategoryIntent? categoryIntent) {
            var filteredBooks = books;

            if (authorIntent != null) {
                filteredBooks = filteredBooks
                    .Where(book => AuthorMatchesIntent(book.Autor, authorIntent))
                    .ToList();
            }

            if (categoryIntent != null) {
                filteredBooks = filteredBooks
                    .Where(book => BookMatchesCategoryIntent(book, categoryIntent))
                    .ToList();
            }

            return filteredBooks;
        }

        private int CalculateDirectMatchScore(Livro book, IReadOnlyCollection<string> tokens) {
            var score = 0;
            var title = NormalizeForMatch(book.Titulo);
            var author = NormalizeForMatch(book.Autor);
            var synopsis = NormalizeForMatch(book.Sinopse);
            var publisher = NormalizeForMatch(book.Editora);
            var categories = book.Categorias?
                .Select(c => NormalizeForMatch(c.Nome))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList() ?? new List<string>();

            foreach (var token in tokens) {
                if (title.Contains(token, StringComparison.OrdinalIgnoreCase)) {
                    score += 120;
                }

                if (author.Contains(token, StringComparison.OrdinalIgnoreCase)) {
                    score += 80;
                }

                if (publisher.Contains(token, StringComparison.OrdinalIgnoreCase)) {
                    score += 35;
                }

                if (synopsis.Contains(token, StringComparison.OrdinalIgnoreCase)) {
                    score += 40;
                }

                if (categories.Any(c => c.Contains(token, StringComparison.OrdinalIgnoreCase))) {
                    score += 60;
                }
            }

            return score;
        }

        private int CalculateScore(
            Livro book,
            IReadOnlyCollection<string> tokens,
            bool hasMeaningfulTokens,
            CustomerProfile customerProfile,
            IReadOnlyDictionary<int, int> popularityByBookId,
            ISet<int> lastRecommendations) {
            var score = 0;
            var author = book.Autor ?? string.Empty;
            var categories = book.Categorias?.Select(c => c.Nome).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            score += CalculateDirectMatchScore(book, tokens);

            foreach (var category in categories) {
                if (customerProfile.CategoryWeights.TryGetValue(category, out var categoryWeight)) {
                    score += categoryWeight * 8;
                }
            }

            if (customerProfile.AuthorWeights.TryGetValue(author, out var authorWeight)) {
                score += authorWeight * 10;
            }

            if (popularityByBookId.TryGetValue(book.Id, out var popularity)) {
                score += Math.Min(popularity * 4, 40);
            }

            if (customerProfile.PurchasedBookIds.Contains(book.Id)) {
                score -= 90;
            }

            if (lastRecommendations.Contains(book.Id)) {
                score -= 15;
            }

            if (!hasMeaningfulTokens) {
                score += 25;
            }

            return score;
        }

        private string BuildReason(
            Livro book,
            string message,
            CustomerProfile customerProfile,
            IReadOnlyDictionary<int, int> popularityByBookId) {
            var tokens = Tokenize(message);
            var normalizedMessage = NormalizeForMatch(message);
            var matchingCategories = (book.Categorias ?? new List<Categoria>())
                .Select(c => c.Nome)
                .Where(name => {
                    var normalizedName = NormalizeForMatch(name);
                    return tokens.Any(token => normalizedName.Contains(token, StringComparison.OrdinalIgnoreCase));
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(name => normalizedMessage.Contains(NormalizeForMatch(name), StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(name => NormalizeForMatch(name).Length)
                .ToList();

            if (matchingCategories.Any()) {
                return $"Combina com a categoria {matchingCategories.First()}.";
            }

            if (tokens.Any(token => NormalizeForMatch(book.Titulo).Contains(token, StringComparison.OrdinalIgnoreCase))) {
                return "Tem aderencia direta ao tema pedido na busca.";
            }

            if (tokens.Any(token => NormalizeForMatch(book.Sinopse).Contains(token, StringComparison.OrdinalIgnoreCase))) {
                return "A sinopse conversa com o assunto que voce pediu.";
            }

            if (customerProfile.AuthorWeights.ContainsKey(book.Autor)) {
                return "Segue o padrao de autor que aparece no historico do cliente.";
            }

            if ((book.Categorias ?? new List<Categoria>()).Any(c => customerProfile.CategoryWeights.ContainsKey(c.Nome))) {
                return "A categoria combina com compras anteriores do cliente.";
            }

            if (popularityByBookId.TryGetValue(book.Id, out var popularity) && popularity > 0) {
                return "Esta entre os livros com melhor saida no catalogo.";
            }

            return "Foi selecionado por combinar com o catalogo ativo e disponivel.";
        }

        private string BuildFallbackReply(
            string message,
            IReadOnlyList<RecommendedBookDto> books,
            CustomerProfile customerProfile) {
            var builder = new StringBuilder();
            builder.Append("Encontrei ");
            builder.Append(books.Count);
            builder.Append(" livro(s) do catalogo que combinam com o seu pedido");

            if (!string.IsNullOrWhiteSpace(message)) {
                builder.Append(" sobre \"");
                builder.Append(message.Trim());
                builder.Append("\"");
            }

            builder.Append(". ");

            if (customerProfile.HasHistory) {
                builder.Append("Tambem considerei o historico de compras do cliente para priorizar os titulos. ");
            }

            builder.Append("Minhas principais sugestoes sao: ");
            builder.Append(string.Join(", ", books.Select(b => b.Title)));
            builder.Append(". Se quiser, eu posso refinar por categoria, autor ou faixa de preco.");

            return builder.ToString();
        }

        private ChatbotResponse? TryBuildActivityContextReply(
            string message,
            IReadOnlyList<ScoredBook> genericBooks,
            CustomerProfile customerProfile,
            IReadOnlyDictionary<int, int> popularityByBookId) {
            var normalized = NormalizeForMatch(message);
            var hasContextTrigger = ContainsAny(normalized, ContextTriggerKeywords);
            var matchedContext = IncompatibleReadingContexts.FirstOrDefault(context =>
                ContainsAny(normalized, context.NormalizedKeywords));

            if (!hasContextTrigger || matchedContext == null) {
                return null;
            }

            var recommendedBooks = genericBooks
                .Select(x => new RecommendedBookDto {
                    Id = x.Book.Id,
                    Title = x.Book.Titulo,
                    Author = x.Book.Autor,
                    Price = $"R$ {x.Book.Preco:N2}",
                    ImageUrl = x.Book.ImagemUrl,
                    DetailsUrl = $"/Home/Detalhes/{x.Book.Id}",
                    Reason = BuildReason(x.Book, string.Empty, customerProfile, popularityByBookId),
                    Categories = x.Book.Categorias?
                        .OrderBy(c => c.Nome)
                        .Select(c => c.Nome)
                        .ToList() ?? new List<string>()
                })
                .ToList();

            var replyBuilder = new StringBuilder();
            replyBuilder.Append(matchedContext.IntroReply);
            replyBuilder.Append(' ');

            if (recommendedBooks.Any()) {
                replyBuilder.Append(matchedContext.RedirectReply);
                replyBuilder.Append(string.Join(", ", recommendedBooks.Select(x => x.Title)));
                replyBuilder.Append('.');
            }
            else {
                replyBuilder.Append("Se quiser, eu posso te indicar livros do catalogo para um momento mais adequado.");
            }

            return new ChatbotResponse {
                Reply = replyBuilder.ToString(),
                UsedAi = false,
                Source = "fallback",
                Recommendations = recommendedBooks
            };
        }

        private ChatbotResponse? TryBuildPurposeMismatchReply(
            string message,
            AuthorIntent? authorIntent,
            CategoryIntent? categoryIntent,
            IReadOnlyList<ScoredBook> genericBooks,
            CustomerProfile customerProfile,
            IReadOnlyDictionary<int, int> popularityByBookId) {
            var normalized = NormalizeForMatch(message);
            var matchedPurpose = IncompatibleLearningPurposes.FirstOrDefault(context =>
                ContainsAny(normalized, context.NormalizedKeywords));
            if (matchedPurpose == null) {
                return null;
            }

            var hasLearningTrigger = ContainsAny(normalized, LearningTriggerKeywords);
            var hasNarrativeIntent = authorIntent != null
                || (categoryIntent != null && IsNarrativeCategoryIntent(categoryIntent));

            if (matchedPurpose.AllowWithoutLearningTrigger) {
                hasNarrativeIntent = true;
            }

            if (!hasNarrativeIntent) {
                return null;
            }

            if (!hasLearningTrigger && !matchedPurpose.AllowWithoutLearningTrigger) {
                return null;
            }

            var recommendedBooks = genericBooks
                .Select(x => new RecommendedBookDto {
                    Id = x.Book.Id,
                    Title = x.Book.Titulo,
                    Author = x.Book.Autor,
                    Price = $"R$ {x.Book.Preco:N2}",
                    ImageUrl = x.Book.ImagemUrl,
                    DetailsUrl = $"/Home/Detalhes/{x.Book.Id}",
                    Reason = BuildReason(x.Book, string.Empty, customerProfile, popularityByBookId),
                    Categories = x.Book.Categorias?
                        .OrderBy(c => c.Nome)
                        .Select(c => c.Nome)
                        .ToList() ?? new List<string>()
                })
                .ToList();

            var replyBuilder = new StringBuilder();
            replyBuilder.Append(matchedPurpose.IntroReply);
            replyBuilder.Append(' ');

            if (recommendedBooks.Any()) {
                replyBuilder.Append(matchedPurpose.RedirectReply);
                replyBuilder.Append(string.Join(", ", recommendedBooks.Select(x => x.Title)));
                replyBuilder.Append('.');
            }
            else {
                replyBuilder.Append("Se quiser, eu posso tentar uma recomendacao mais alinhada ao genero, autor ou tema de leitura que voce procura.");
            }

            return new ChatbotResponse {
                Reply = replyBuilder.ToString(),
                UsedAi = false,
                Source = "fallback",
                Recommendations = recommendedBooks
            };
        }

        private async Task<string?> GenerateAiReplyAsync(
            string message,
            IReadOnlyList<RecommendedBookDto> books,
            CustomerProfile customerProfile,
            ChatbotSessionState sessionState,
            CancellationToken cancellationToken) {
            var apiKey = _openAiOptions.ApiKey.Trim();
            if (string.IsNullOrWhiteSpace(apiKey)) {
                return null;
            }

            var previousTurns = string.Join(Environment.NewLine, sessionState.Turns
                .TakeLast(6)
                .Select(t => $"{t.Role}: {t.Message}"));

            var recommendationLines = string.Join(
                Environment.NewLine,
                books.Select(book =>
                    $"- Id {book.Id} | Titulo: {book.Title} | Autor: {book.Author} | Categorias: {(book.Categories.Any() ? string.Join(", ", book.Categories) : "Sem categoria")} | Preco: {book.Price} | Motivo: {book.Reason}"));

            var historySummary = customerProfile.HasHistory
                ? $"Categorias mais compradas: {string.Join(", ", customerProfile.CategoryWeights.OrderByDescending(x => x.Value).Take(3).Select(x => x.Key))}. Autores recorrentes: {string.Join(", ", customerProfile.AuthorWeights.OrderByDescending(x => x.Value).Take(3).Select(x => x.Key))}."
                : "Cliente sem historico de compras identificado.";

            var systemPrompt = """
Voce e um assistente virtual de um e-commerce de livros.
Responda em portugues do Brasil e em texto simples.
Use apenas os livros fornecidos pelo sistema.
Nunca invente livros, autores, categorias, preco, estoque ou beneficios.
Nao recomende nenhum titulo fora da lista recebida.
Se a pergunta do usuario estiver fora do contexto de livros do catalogo, explique com educacao que so pode ajudar com livros e recomendacoes do catalogo.
Prefira respostas curtas, claras e objetivas.
""";

            var userPrompt = $"""
Pergunta do usuario:
{message}

Historico resumido do cliente:
{historySummary}

Ultimas mensagens da conversa:
{(string.IsNullOrWhiteSpace(previousTurns) ? "Sem historico anterior." : previousTurns)}

Livros candidatos aprovados pelo sistema:
{recommendationLines}

Escreva uma resposta curta recomendando entre 1 e 3 livros dessa lista e explique o motivo sem usar markdown.
""";

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new OpenAiResponsesRequest {
                Model = _openAiOptions.Model,
                Input = new List<OpenAiInputMessage> {
                    OpenAiInputMessage.Create("system", systemPrompt),
                    OpenAiInputMessage.Create("user", userPrompt)
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode) {
                _logger.LogWarning("OpenAI retornou status {StatusCode}: {Body}", response.StatusCode, content);
                return null;
            }

            var responseBody = JsonSerializer.Deserialize<OpenAiResponsesResponse>(content);
            var reply = responseBody?.Output?
                .SelectMany(item => item.Content ?? new List<OpenAiOutputContent>())
                .Where(item => string.Equals(item.Type, "output_text", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.Type, "text", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

            return reply;
        }

        private void UpdateSessionState(ChatbotSessionState state, string userMessage, string assistantMessage, IEnumerable<int> bookIds) {
            state.Turns.Add(new ChatbotTurn {
                Role = "usuario",
                Message = userMessage.Trim()
            });

            state.Turns.Add(new ChatbotTurn {
                Role = "assistente",
                Message = assistantMessage.Trim()
            });

            state.LastRecommendedBookIds = bookIds.Distinct().Take(5).ToList();
            state.Turns = state.Turns.TakeLast(8).ToList();
        }

        private static HashSet<string> Tokenize(string text) {
            return text
                .Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}', '"' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeForMatch)
                .Where(token => token.Length >= 3 && !StopWords.Contains(token))
                .ToHashSet();
        }

        private static bool ContainsAny(string text, IEnumerable<string> candidates) {
            return candidates.Any(candidate => text.Contains(candidate, StringComparison.OrdinalIgnoreCase));
        }

        private static AuthorIntent? TryExtractAuthorIntent(string message, IEnumerable<Livro> books) {
            if (string.IsNullOrWhiteSpace(message)) {
                return null;
            }

            var patterns = new[] {
                new AuthorPattern(@"escrit(?:o|os|a|as)\s+por\s+(?<author>.+)$", false),
                new AuthorPattern(@"livros?\s+do\s+autor\s+(?<author>.+)$", false),
                new AuthorPattern(@"livros?\s+da\s+autora\s+(?<author>.+)$", false),
                new AuthorPattern(@"livros?\s+do\s+(?<author>.+)$", true),
                new AuthorPattern(@"livros?\s+da\s+(?<author>.+)$", true),
                new AuthorPattern(@"autor(?:a)?\s+(?<author>.+)$", false),
                new AuthorPattern(@"livros?\s+de\s+(?<author>.+)$", true)
            };

            foreach (var pattern in patterns) {
                var match = Regex.Match(message.Trim(), pattern.Pattern, RegexOptions.IgnoreCase);
                if (!match.Success) {
                    continue;
                }

                var displayText = match.Groups["author"].Value.Trim(' ', '.', '!', '?', '"');
                displayText = SanitizeAuthorCandidate(displayText);
                if (string.IsNullOrWhiteSpace(displayText)) {
                    continue;
                }

                var resolvedAuthor = ResolveAuthorFromCatalog(displayText, books);
                if (pattern.RequireCatalogMatch && resolvedAuthor == null) {
                    continue;
                }

                return new AuthorIntent {
                    DisplayText = resolvedAuthor ?? displayText,
                    NormalizedText = NormalizeForMatch(resolvedAuthor ?? displayText),
                    Tokens = Tokenize(resolvedAuthor ?? displayText).ToList()
                };
            }

            return null;
        }

        private static CategoryIntent? TryExtractCategoryIntent(string message, IEnumerable<Livro> books) {
            if (string.IsNullOrWhiteSpace(message)) {
                return null;
            }

            var normalizedMessage = NormalizeForMatch(message);

            var patterns = new[] {
                @"categoria\s+(?<category>.+)$",
                @"genero\s+(?<category>.+)$",
                @"livros?\s+de\s+(?<category>.+)$",
                @"algo\s+de\s+(?<category>.+)$"
            };

            foreach (var pattern in patterns) {
                var match = Regex.Match(normalizedMessage, pattern, RegexOptions.IgnoreCase);
                if (!match.Success) {
                    continue;
                }

                var displayText = match.Groups["category"].Value.Trim(' ', '.', '!', '?', '"');
                if (string.IsNullOrWhiteSpace(displayText)) {
                    continue;
                }

                var resolvedCategory = ResolveCategoryFromCatalog(displayText, books);
                if (resolvedCategory == null) {
                    continue;
                }

                return new CategoryIntent {
                    DisplayText = resolvedCategory,
                    NormalizedText = NormalizeForMatch(resolvedCategory),
                    Tokens = Tokenize(resolvedCategory).ToList()
                };
            }

            var resolvedFromWholeMessage = ResolveCategoryFromCatalog(normalizedMessage, books);
            if (resolvedFromWholeMessage != null) {
                return new CategoryIntent {
                    DisplayText = resolvedFromWholeMessage,
                    NormalizedText = NormalizeForMatch(resolvedFromWholeMessage),
                    Tokens = Tokenize(resolvedFromWholeMessage).ToList()
                };
            }

            return null;
        }

        private static bool AuthorMatchesIntent(string? authorName, AuthorIntent authorIntent) {
            if (string.IsNullOrWhiteSpace(authorName)) {
                return false;
            }

            var normalizedAuthor = NormalizeForMatch(authorName);

            if (normalizedAuthor.Contains(authorIntent.NormalizedText, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            return authorIntent.Tokens.All(token =>
                normalizedAuthor.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        private static bool BookMatchesCategoryIntent(Livro book, CategoryIntent categoryIntent) {
            var normalizedCategories = (book.Categorias ?? new List<Categoria>())
                .Select(category => NormalizeForMatch(category.Nome))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            if (!normalizedCategories.Any()) {
                return false;
            }

            if (normalizedCategories.Any(name =>
                name.Contains(categoryIntent.NormalizedText, StringComparison.OrdinalIgnoreCase))) {
                return true;
            }

            return categoryIntent.Tokens.All(token =>
                normalizedCategories.Any(name => name.Contains(token, StringComparison.OrdinalIgnoreCase)));
        }

        private static bool IsNarrativeCategoryIntent(CategoryIntent categoryIntent) {
            if (NarrativeCategoryKeywords.Contains(categoryIntent.NormalizedText)) {
                return true;
            }

            return categoryIntent.Tokens.Any(token => NarrativeCategoryKeywords.Contains(token));
        }

        private static string NormalizeForMatch(string? text) {
            if (string.IsNullOrWhiteSpace(text)) {
                return string.Empty;
            }

            var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var character in normalized) {
                if (char.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark) {
                    builder.Append(character);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string SanitizeAuthorCandidate(string text) {
            if (string.IsNullOrWhiteSpace(text)) {
                return string.Empty;
            }

            var sanitized = Regex.Replace(
                text,
                @"\s+(?:na\s+categoria|no\s+genero|na\s+area|em\s+categoria|categoria|genero)\s+.+$",
                string.Empty,
                RegexOptions.IgnoreCase);

            sanitized = Regex.Replace(
                sanitized,
                @"\s+(?:para\s+aprender|pra\s+aprender|para\s+ganhar|pra\s+ganhar|para\s+ensinar|pra\s+ensinar|que\s+ensine|que\s+ensina|para\s+consertar|pra\s+consertar|para\s+dirigir|pra\s+dirigir|para\s+pilotar|pra\s+pilotar|para\s+nadar|pra\s+nadar)\s+.+$",
                string.Empty,
                RegexOptions.IgnoreCase);

            return sanitized.Trim(' ', '.', '!', '?', '"');
        }

        private static string? ResolveAuthorFromCatalog(string candidateText, IEnumerable<Livro> books) {
            var normalizedCandidate = NormalizeForMatch(candidateText);
            var candidateTokens = Tokenize(candidateText).ToList();
            if (!candidateTokens.Any()) {
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

            var containedMatch = authors.FirstOrDefault(author =>
                author.Normalized.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase));
            if (containedMatch != null) {
                return containedMatch.Original;
            }

            var tokenMatch = authors
                .Where(author => candidateTokens.All(token =>
                    author.Normalized.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(author => author.Normalized.Length)
                .FirstOrDefault();

            return tokenMatch?.Original;
        }

        private static string? ResolveCategoryFromCatalog(string candidateText, IEnumerable<Livro> books) {
            var normalizedCandidate = NormalizeForMatch(candidateText);
            var candidateTokens = Tokenize(candidateText).ToList();
            if (string.IsNullOrWhiteSpace(normalizedCandidate)) {
                return null;
            }

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
                .Where(category => normalizedCandidate.Contains(category.Normalized, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(category => category.Normalized.Length)
                .FirstOrDefault();

            if (phraseMatch != null) {
                return phraseMatch.Original;
            }

            if (!candidateTokens.Any()) {
                return null;
            }

            var containedMatch = categories
                .Where(category => category.Normalized.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
                .OrderBy(category => category.Normalized.Length)
                .FirstOrDefault();

            if (containedMatch != null) {
                return containedMatch.Original;
            }

            var tokenMatch = categories
                .Where(category => candidateTokens.All(token =>
                    category.Normalized.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(category => category.Normalized.Count(character => character == ' '))
                .ThenByDescending(category => category.Normalized.Length)
                .FirstOrDefault();

            return tokenMatch?.Original;
        }

        private sealed class ScoredBook {
            public required Livro Book { get; set; }
            public int DirectMatchScore { get; set; }
            public int Score { get; set; }
        }

        private sealed class CustomerProfile {
            public static CustomerProfile Empty { get; } = new();

            public bool HasHistory => PurchasedBookIds.Count > 0;
            public HashSet<int> PurchasedBookIds { get; init; } = new();
            public Dictionary<string, int> CategoryWeights { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> AuthorWeights { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class AuthorIntent {
            public string DisplayText { get; init; } = string.Empty;
            public string NormalizedText { get; init; } = string.Empty;
            public List<string> Tokens { get; init; } = new();
        }

        private sealed class CategoryIntent {
            public string DisplayText { get; init; } = string.Empty;
            public string NormalizedText { get; init; } = string.Empty;
            public List<string> Tokens { get; init; } = new();
        }

        private sealed record AuthorPattern(string Pattern, bool RequireCatalogMatch);
        private sealed record ContextFamily(string DisplayName, string IntroReply, string RedirectReply, IReadOnlyList<string> Keywords) {
            public IReadOnlyList<string> NormalizedKeywords { get; } = Keywords
                .Select(NormalizeForMatch)
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                .ToList();
        }
        private sealed record PurposeMismatchFamily(string DisplayName, string IntroReply, string RedirectReply, bool AllowWithoutLearningTrigger, IReadOnlyList<string> Keywords) {
            public IReadOnlyList<string> NormalizedKeywords { get; } = Keywords
                .Select(NormalizeForMatch)
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                .ToList();
        }

        private sealed class OpenAiResponsesRequest {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("input")]
            public List<OpenAiInputMessage> Input { get; set; } = new();
        }

        private sealed class OpenAiInputMessage {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public List<OpenAiInputText> Content { get; set; } = new();

            public static OpenAiInputMessage Create(string role, string text) {
                return new OpenAiInputMessage {
                    Role = role,
                    Content = new List<OpenAiInputText> {
                        new() {
                            Type = "input_text",
                            Text = text
                        }
                    }
                };
            }
        }

        private sealed class OpenAiInputText {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "input_text";

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        private sealed class OpenAiResponsesResponse {
            [JsonPropertyName("output")]
            public List<OpenAiOutputItem>? Output { get; set; }
        }

        private sealed class OpenAiOutputItem {
            [JsonPropertyName("content")]
            public List<OpenAiOutputContent>? Content { get; set; }
        }

        private sealed class OpenAiOutputContent {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}
