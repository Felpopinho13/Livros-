using Livros.Domain;

namespace Livros.Application.Recommendations {
    public interface ILivroRecommendationDataProvider {
        Task<List<Livro>> LoadAvailableBooksAsync(CancellationToken cancellationToken = default);
        Task<LivroRecommendationCustomerProfile> BuildCustomerProfileAsync(int? clienteId, CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetPopularityByBookIdAsync(CancellationToken cancellationToken = default);
    }
}
