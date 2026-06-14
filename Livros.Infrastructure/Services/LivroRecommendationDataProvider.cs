using Livros.Application.Recommendations;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class LivroRecommendationDataProvider : ILivroRecommendationDataProvider {
        private static readonly HashSet<string> InvalidOrderStatuses = new(StringComparer.OrdinalIgnoreCase) {
            "REPROVADA",
            "PAGAMENTO RECUSADO",
            "CANCELADO"
        };

        private readonly AppDbContext _context;

        public LivroRecommendationDataProvider(AppDbContext context) {
            _context = context;
        }

        public async Task<List<Livro>> LoadAvailableBooksAsync(CancellationToken cancellationToken = default) {
            return await _context.Livros
                .AsNoTracking()
                .Include(book => book.Categorias)
                .Include(book => book.Estoque)
                .Where(book => book.IsAtivo && book.Estoque != null && book.Estoque.Quantidade > 0)
                .OrderBy(book => book.Titulo)
                .ToListAsync(cancellationToken);
        }

        public async Task<LivroRecommendationCustomerProfile> BuildCustomerProfileAsync(int? clienteId, CancellationToken cancellationToken = default) {
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

        public async Task<Dictionary<int, int>> GetPopularityByBookIdAsync(CancellationToken cancellationToken = default) {
            return await _context.PedidoItens
                .AsNoTracking()
                .Include(item => item.Pedido)
                .Where(item => !InvalidOrderStatuses.Contains(item.Pedido.Status))
                .GroupBy(item => item.LivroId)
                .Select(group => new { LivroId = group.Key, Quantity = group.Sum(item => item.Quantidade) })
                .ToDictionaryAsync(item => item.LivroId, item => item.Quantity, cancellationToken);
        }
    }
}
