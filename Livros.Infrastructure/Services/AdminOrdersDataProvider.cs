using Livros.Application.AdminOrders;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class AdminOrdersDataProvider : IAdminOrdersDataProvider {
        private readonly AppDbContext _context;

        public AdminOrdersDataProvider(AppDbContext context) {
            _context = context;
        }

        public async Task<AdminOrdersPageData> LoadPageAsync(AdminOrdersQuery query, int pageSize, CancellationToken cancellationToken = default) {
            var filteredQuery = ApplyFilters(_context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Endereco)
                    .ThenInclude(e => e.Cidade)
                        .ThenInclude(c => c.Estado)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .Include(p => p.Pagamentos), query);

            var totalPedidos = await filteredQuery.CountAsync(cancellationToken);
            var pedidos = await filteredQuery
                .OrderByDescending(p => p.Data)
                .Skip((query.Pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new AdminOrdersPageData {
                Pedidos = pedidos,
                TotalPedidos = totalPedidos
            };
        }

        public async Task<Dictionary<int, int>> LoadTradeCountsAsync(IReadOnlyCollection<int> pedidoIds, CancellationToken cancellationToken = default) {
            if (pedidoIds.Count == 0) {
                return new Dictionary<int, int>();
            }

            return await _context.Trocas
                .AsNoTracking()
                .Where(t => pedidoIds.Contains(t.PedidoId))
                .GroupBy(t => t.PedidoId)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
        }

        public async Task<Pedido?> LoadForStatusUpdateAsync(int pedidoId, CancellationToken cancellationToken = default) {
            return await _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .Include(p => p.Pagamentos)
                .FirstOrDefaultAsync(p => p.Id == pedidoId, cancellationToken);
        }

        public async Task<Dictionary<int, Estoque>> LoadStocksForBooksAsync(IReadOnlyCollection<int> livroIds, CancellationToken cancellationToken = default) {
            if (livroIds.Count == 0) {
                return new Dictionary<int, Estoque>();
            }

            return await _context.Estoques
                .Where(e => livroIds.Contains(e.LivroId))
                .ToDictionaryAsync(e => e.LivroId, cancellationToken);
        }

        public Estoque CreateStock(int livroId) {
            var estoque = new Estoque {
                LivroId = livroId,
                Quantidade = 0
            };

            _context.Estoques.Add(estoque);
            return estoque;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private static IQueryable<Pedido> ApplyFilters(IQueryable<Pedido> query, AdminOrdersQuery filters) {
            if (!string.IsNullOrWhiteSpace(filters.Busca)) {
                var buscaNormalizada = filters.Busca.Trim();
                query = query.Where(p =>
                    p.Id.ToString().Contains(buscaNormalizada) ||
                    p.Cliente.Nome.Contains(buscaNormalizada) ||
                    p.Cliente.Email.Contains(buscaNormalizada) ||
                    p.Itens.Any(i => i.Livro.Titulo.Contains(buscaNormalizada)));
            }

            if (!string.IsNullOrWhiteSpace(filters.Status)) {
                var statusEquivalentes = ObterStatusEquivalentesFiltroPedido(filters.Status);
                query = query.Where(p => statusEquivalentes.Contains(p.Status));
            }

            return query;
        }

        private static string NormalizarStatusPedidoInterno(string? statusAtual) {
            return (statusAtual ?? string.Empty).Trim().ToUpperInvariant() switch {
                "EM PROCESSAMENTO" => "APROVADA",
                "PAGAMENTO APROVADO" => "APROVADA",
                "PAGAMENTO RECUSADO" => "REPROVADA",
                "ENVIADO" => "EM TRANSPORTE",
                var status => status
            };
        }

        private static string[] ObterStatusEquivalentesFiltroPedido(string status) {
            return NormalizarStatusPedidoInterno(status) switch {
                "APROVADA" => new[] { "APROVADA", "PAGAMENTO APROVADO", "EM PROCESSAMENTO" },
                "REPROVADA" => new[] { "REPROVADA", "PAGAMENTO RECUSADO" },
                "EM TRANSPORTE" => new[] { "EM TRANSPORTE", "ENVIADO" },
                var statusNormalizado => new[] { statusNormalizado }
            };
        }
    }
}
