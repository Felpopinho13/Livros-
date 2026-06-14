using Livros.Application.Recommendations;
using Livros.Web.Configuration;
using Livros.Web.Models.Chatbot;
using Microsoft.Extensions.Options;

namespace Livros.Web.Services {
    public sealed class LivroRecommendationChatService {
        private readonly ILivroRecommendationDataProvider _dataProvider;
        private readonly HttpClient _httpClient;
        private readonly OpenAiOptions _openAiOptions;
        private readonly ILogger<LivroRecommendationChatService> _logger;

        public LivroRecommendationChatService(
            ILivroRecommendationDataProvider dataProvider,
            HttpClient httpClient,
            IOptions<OpenAiOptions> openAiOptions,
            ILogger<LivroRecommendationChatService> logger) {
            _dataProvider = dataProvider;
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

            var books = await _dataProvider.LoadAvailableBooksAsync(cancellationToken);
            if (!books.Any()) {
                return BuildSimpleResponse("Nao ha livros disponiveis no catalogo neste momento.");
            }

            var customerProfile = await _dataProvider.BuildCustomerProfileAsync(clienteId, cancellationToken);
            var popularityByBookId = await _dataProvider.GetPopularityByBookIdAsync(cancellationToken);

            var intent = LivroRecommendationTextHelper.ExtractIntent(trimmedMessage, books);
            var candidates = LivroRecommendationEngine.FilterCandidateBooks(books, intent);
            var scoredCandidates = LivroRecommendationEngine.ScoreBooks(
                candidates,
                trimmedMessage,
                customerProfile,
                popularityByBookId,
                sessionState.LastRecommendedBookIds);
            var selectedCandidates = LivroRecommendationEngine.SelectCandidates(scoredCandidates, intent);

            if (!selectedCandidates.Any()) {
                var noResultReply = intent.HasCatalogConstraint
                    ? LivroRecommendationEngine.BuildNoResultReply(intent)
                    : "Desculpe, nao tenho informacoes sobre isso. Estou aqui para ajudar com livros do catalogo.";

                UpdateSessionState(sessionState, trimmedMessage, noResultReply, Array.Empty<int>());
                return BuildSimpleResponse(noResultReply);
            }

            var suggestions = LivroRecommendationEngine.BuildSuggestions(
                selectedCandidates,
                trimmedMessage,
                customerProfile,
                popularityByBookId);
            var recommendations = LivroRecommendationResponseBuilder.MapRecommendations(suggestions);

            var fallbackSafe = LivroRecommendationEngine.IsFallbackRecommendationSafe(
                trimmedMessage,
                selectedCandidates,
                intent);

            var responseRecommendations = fallbackSafe
                ? recommendations
                : new List<RecommendedBookDto>();

            var reply = fallbackSafe
                ? LivroRecommendationEngine.BuildFallbackReply(trimmedMessage, suggestions, intent, customerProfile)
                : LivroRecommendationEngine.BuildLowConfidenceReply(intent);

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
