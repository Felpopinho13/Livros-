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
            var trimmedMessage = message?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedMessage)) {
                return BuildSimpleResponse("Mensagem obrigatoria.");
            }

            var books = await LoadAvailableBooksAsync(cancellationToken);
            if (!books.Any()) {
                return BuildSimpleResponse("Nao ha livros disponiveis no catalogo neste momento.");
            }

            var customerProfile = await BuildCustomerProfileAsync(clienteId, cancellationToken);
            var popularityByBookId = await GetPopularityByBookIdAsync(cancellationToken);

            var intent = LivroRecommendationTextHelper.ExtractIntent(trimmedMessage, books);
            var candidates = LivroRecommendationCatalogEngine.FilterCandidateBooks(books, intent);
            var scoredCandidates = LivroRecommendationCatalogEngine.ScoreBooks(
                candidates,
                trimmedMessage,
                customerProfile,
                popularityByBookId,
                sessionState);
            var selectedCandidates = LivroRecommendationCatalogEngine.SelectCandidates(scoredCandidates, intent);

            if (!selectedCandidates.Any()) {
                var noResultReply = intent.HasCatalogConstraint
                    ? LivroRecommendationCatalogEngine.BuildNoResultReply(intent)
                    : "Desculpe, nao tenho informacoes sobre isso. Estou aqui para ajudar com livros do catalogo.";

                UpdateSessionState(sessionState, trimmedMessage, noResultReply, Array.Empty<int>());
                return BuildSimpleResponse(noResultReply);
            }

            var recommendations = LivroRecommendationCatalogEngine.MapRecommendations(
                selectedCandidates,
                trimmedMessage,
                customerProfile,
                popularityByBookId);

            var fallbackSafe = LivroRecommendationCatalogEngine.IsFallbackRecommendationSafe(
                trimmedMessage,
                selectedCandidates,
                intent);

            var responseRecommendations = fallbackSafe
                ? recommendations
                : new List<RecommendedBookDto>();

            var reply = fallbackSafe
                ? LivroRecommendationCatalogEngine.BuildFallbackReply(trimmedMessage, recommendations, intent, customerProfile)
                : LivroRecommendationCatalogEngine.BuildLowConfidenceReply(intent);

            var usedAi = false;
            var source = "fallback";

            if (LivroRecommendationOpenAiClient.CanUse(_openAiOptions)) {
                try {
                    var aiReply = await LivroRecommendationOpenAiClient.GenerateReplyAsync(
                        _httpClient,
                        _openAiOptions,
                        _logger,
                        trimmedMessage,
                        recommendations,
                        customerProfile,
                        sessionState,
                        cancellationToken);

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

            UpdateSessionState(sessionState, trimmedMessage, reply, responseRecommendations.Select(book => book.Id));

            return new ChatbotResponse {
                Reply = reply,
                UsedAi = usedAi,
                Source = source,
                Recommendations = responseRecommendations
            };
        }

        private async Task<List<Livro>> LoadAvailableBooksAsync(CancellationToken cancellationToken) {
            return await _context.Livros
                .AsNoTracking()
                .Include(book => book.Categorias)
                .Include(book => book.Estoque)
                .Where(book => book.IsAtivo && book.Estoque != null && book.Estoque.Quantidade > 0)
                .OrderBy(book => book.Titulo)
                .ToListAsync(cancellationToken);
        }

        private async Task<LivroRecommendationCustomerProfile> BuildCustomerProfileAsync(int? clienteId, CancellationToken cancellationToken) {
            if (!clienteId.HasValue) {
                return LivroRecommendationCustomerProfile.Empty;
            }

            var purchaseItems = await _context.PedidoItens
                .AsNoTracking()
                .Include(item => item.Pedido)
                .Include(item => item.Livro)
                    .ThenInclude(book => book.Categorias)
                .Where(item => item.Pedido.ClienteId == clienteId.Value && !InvalidOrderStatuses.Contains(item.Pedido.Status))
                .ToListAsync(cancellationToken);

            var purchasedBookIds = purchaseItems
                .Select(item => item.LivroId)
                .Distinct()
                .ToHashSet();

            var categoryWeights = purchaseItems
                .SelectMany(item => (item.Livro.Categorias ?? new List<Categoria>())
                    .Select(category => new { category.Nome, Weight = item.Quantidade }))
                .GroupBy(item => item.Nome)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Weight), StringComparer.OrdinalIgnoreCase);

            var authorWeights = purchaseItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Livro.Autor))
                .GroupBy(item => item.Livro.Autor)
                .ToDictionary(group => group.Key!, group => group.Sum(item => item.Quantidade), StringComparer.OrdinalIgnoreCase);

            return new LivroRecommendationCustomerProfile {
                PurchasedBookIds = purchasedBookIds,
                CategoryWeights = categoryWeights,
                AuthorWeights = authorWeights
            };
        }

        private async Task<Dictionary<int, int>> GetPopularityByBookIdAsync(CancellationToken cancellationToken) {
            return await _context.PedidoItens
                .AsNoTracking()
                .Include(item => item.Pedido)
                .Where(item => !InvalidOrderStatuses.Contains(item.Pedido.Status))
                .GroupBy(item => item.LivroId)
                .Select(group => new { LivroId = group.Key, Quantity = group.Sum(item => item.Quantidade) })
                .ToDictionaryAsync(item => item.LivroId, item => item.Quantity, cancellationToken);
        }

        private static ChatbotResponse BuildSimpleResponse(string reply) {
            return new ChatbotResponse {
                Reply = reply,
                UsedAi = false,
                Source = "fallback",
                Recommendations = new List<RecommendedBookDto>()
            };
        }

        private static void UpdateSessionState(
            ChatbotSessionState state,
            string userMessage,
            string assistantMessage,
            IEnumerable<int> bookIds) {
            state.Turns.Add(new ChatbotTurn {
                Role = "usuario",
                Message = userMessage.Trim()
            });

            state.Turns.Add(new ChatbotTurn {
                Role = "assistente",
                Message = assistantMessage.Trim()
            });

            state.LastRecommendedBookIds = bookIds
                .Distinct()
                .Take(5)
                .ToList();

            state.Turns = state.Turns.TakeLast(8).ToList();
        }
    }
}
