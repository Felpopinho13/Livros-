using Livros.Application.SalesAnalysis;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class SalesAnalysisDataProvider : ISalesAnalysisDataProvider {
        private static readonly string[] EligibleStatuses = {
            "APROVADA",
            "PAGAMENTO APROVADO",
            "EM SEPARACAO",
            "EM TRANSPORTE",
            "ENVIADO",
            "ENTREGUE"
        };

        private readonly AppDbContext _context;

        public SalesAnalysisDataProvider(AppDbContext context) {
            _context = context;
        }

        public async Task<List<SalesAnalysisCategoryOption>> LoadCategoryOptionsAsync(CancellationToken cancellationToken = default) {
            return await _context.Categorias
                .AsNoTracking()
                .OrderBy(c => c.Nome)
                .Select(c => new SalesAnalysisCategoryOption {
                    Id = c.Id,
                    Nome = c.Nome
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<PedidoItem>> LoadSoldItemsAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default) {
            var fimExclusivo = fim.Date.AddDays(1);

            return await _context.PedidoItens
                .AsNoTracking()
                .Include(i => i.Pedido)
                .Include(i => i.Livro)
                    .ThenInclude(l => l.Categorias)
                .Where(i =>
                    i.Pedido != null &&
                    EligibleStatuses.Contains(i.Pedido.Status) &&
                    i.Pedido.Data >= inicio.Date &&
                    i.Pedido.Data < fimExclusivo)
                .ToListAsync(cancellationToken);
        }
    }
}
