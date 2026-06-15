namespace Livros.Application.Recommendations {
    public interface ILivroRecommendationAiClient {
        Task<string?> GenerateReplyAsync(
            string message,
            IReadOnlyList<RecommendedBookDto> books,
            LivroRecommendationCustomerProfile customerProfile,
            ChatbotSessionState sessionState,
            CancellationToken cancellationToken = default);
    }
}
