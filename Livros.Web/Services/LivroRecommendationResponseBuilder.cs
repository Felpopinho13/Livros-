using Livros.Application.Recommendations;
using Livros.Web.Models.Chatbot;

namespace Livros.Web.Services {
    internal static class LivroRecommendationResponseBuilder {
        public static List<RecommendedBookDto> MapRecommendations(
            IReadOnlyList<LivroRecommendationSuggestion> suggestions) {
            return suggestions
                .Select(item => new RecommendedBookDto {
                    Id = item.Book.Id,
                    Title = item.Book.Titulo,
                    Author = item.Book.Autor,
                    Price = $"R$ {item.Book.Preco:N2}",
                    ImageUrl = item.Book.ImagemUrl,
                    DetailsUrl = $"/Home/Detalhes/{item.Book.Id}",
                    Reason = item.Reason,
                    Categories = item.Categories
                })
                .ToList();
        }
    }
}
