namespace Livros.Application.Recommendations {
    public sealed class LivroRecommendationChatService {
        private readonly ILivroRecommendationDataProvider _dataProvider;
        private readonly ILivroRecommendationAiClient _aiClient;

        public LivroRecommendationChatService(
            ILivroRecommendationDataProvider dataProvider,
            ILivroRecommendationAiClient aiClient) {
            _dataProvider = dataProvider;
            _aiClient = aiClient;
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
            var recommendations = MapRecommendations(suggestions);

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

            if (responseRecommendations.Any()) {
                var aiReply = await _aiClient.GenerateReplyAsync(
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

            UpdateSessionState(sessionState, trimmedMessage, reply, responseRecommendations.Select(book => book.Id));

            return new ChatbotResponse {
                Reply = reply,
                UsedAi = usedAi,
                Source = source,
                Recommendations = responseRecommendations
            };
        }

        private static List<RecommendedBookDto> MapRecommendations(
            IReadOnlyList<LivroRecommendationSuggestion> suggestions) {
            return suggestions
                .Select(item => new RecommendedBookDto {
                    Id = item.Book.Id,
                    Title = item.Book.Titulo,
                    Author = item.Book.Autor,
                    Price = item.Book.Preco,
                    ImageUrl = item.Book.ImagemUrl,
                    Reason = item.Reason,
                    Categories = item.Categories
                })
                .ToList();
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
