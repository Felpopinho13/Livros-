using Livros.Application.AdminExchanges;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class AdminExchangesDataProvider : IAdminExchangesDataProvider {
        private readonly AppDbContext _context;

        public AdminExchangesDataProvider(AppDbContext context) {
            _context = context;
        }

        public async Task<AdminExchangesPageData> LoadPageAsync(AdminExchangesQuery query, int pageSize, CancellationToken cancellationToken = default) {
            var trocasQuery = ApplyTradeFilters(_context.Trocas
                .AsNoTracking()
                .Include(t => t.Cliente)
                .Include(t => t.Pedido)
                .Include(t => t.PedidoItem)
                    .ThenInclude(i => i.Livro)
                .Include(t => t.CupomDesconto), query);

            var totalTrocas = await trocasQuery.CountAsync(cancellationToken);
            var trocas = await trocasQuery
                .OrderByDescending(t => t.DataSolicitacao)
                .Skip((query.PaginaTrocas - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var cuponsBaseQuery = _context.CuponsDesconto
                .AsNoTracking()
                .Include(c => c.Cliente)
                .OrderByDescending(c => c.DataCriacao);

            var totalCupons = await cuponsBaseQuery.CountAsync(cancellationToken);
            var cuponsPagina = await cuponsBaseQuery
                .Skip((query.PaginaCupons - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var cuponsRecentes = await _context.CuponsDesconto
                .AsNoTracking()
                .OrderByDescending(c => c.DataCriacao)
                .Take(8)
                .ToListAsync(cancellationToken);

            var clientesAtivos = await GetActiveCustomersAsync(cancellationToken);
            var valoresSugeridos = await CalcularValoresSugeridosAsync(trocas, cancellationToken);

            return new AdminExchangesPageData {
                Trocas = trocas,
                TotalTrocas = totalTrocas,
                CuponsPagina = cuponsPagina,
                TotalCupons = totalCupons,
                CuponsRecentes = cuponsRecentes,
                ClientesAtivos = clientesAtivos,
                ValoresSugeridosCupomPorTroca = valoresSugeridos
            };
        }

        public async Task<Troca?> GetTradeForUpdateAsync(int trocaId, CancellationToken cancellationToken = default) {
            return await _context.Trocas
                .Include(t => t.Pedido)
                .Include(t => t.PedidoItem)
                    .ThenInclude(i => i.Livro)
                .Include(t => t.CupomDesconto)
                .FirstOrDefaultAsync(t => t.Id == trocaId, cancellationToken);
        }

        public async Task<decimal> CalculateSuggestedCouponValueAsync(Troca troca, CancellationToken cancellationToken = default) {
            if (troca.PedidoItem == null || troca.Pedido == null) {
                return 0;
            }

            var subtotalPedido = await _context.PedidoItens
                .AsNoTracking()
                .Where(i => i.PedidoId == troca.Pedido.Id)
                .SumAsync(i => i.PrecoUnitario * i.Quantidade, cancellationToken);

            var descontoPedido = await _context.CuponsDesconto
                .AsNoTracking()
                .Where(c => c.PedidoId == troca.Pedido.Id)
                .SumAsync(c => c.Valor, cancellationToken);

            var totalItem = troca.PedidoItem.PrecoUnitario * troca.PedidoItem.Quantidade;
            if (subtotalPedido <= 0) {
                return decimal.Round(totalItem, 2);
            }

            var fretePedido = Math.Max(troca.Pedido.Total - subtotalPedido + descontoPedido, 0);
            var proporcaoItem = totalItem / subtotalPedido;
            var freteProporcional = decimal.Round(fretePedido * proporcaoItem, 2);
            return decimal.Round(totalItem + freteProporcional, 2);
        }

        public async Task ReintegrateTradeItemToStockAsync(PedidoItem? pedidoItem, CancellationToken cancellationToken = default) {
            if (pedidoItem == null) {
                return;
            }

            var estoque = await _context.Estoques.FirstOrDefaultAsync(e => e.LivroId == pedidoItem.LivroId, cancellationToken);
            if (estoque == null) {
                estoque = new Estoque {
                    LivroId = pedidoItem.LivroId,
                    Quantidade = 0
                };
                await _context.Estoques.AddAsync(estoque, cancellationToken);
            }

            estoque.Quantidade += pedidoItem.Quantidade;
        }

        public async Task<CupomDesconto> CreateCouponAsync(CupomDesconto cupom, CancellationToken cancellationToken = default) {
            await _context.CuponsDesconto.AddAsync(cupom, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return cupom;
        }

        public async Task CreateCouponsAsync(IEnumerable<CupomDesconto> cupons, CancellationToken cancellationToken = default) {
            var lista = cupons.ToList();
            if (lista.Count == 0) {
                return;
            }

            await _context.CuponsDesconto.AddRangeAsync(lista, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Cliente>> GetActiveCustomersAsync(CancellationToken cancellationToken = default) {
            return await _context.Clientes
                .AsNoTracking()
                .Where(c => c.IsAtivo)
                .OrderBy(c => c.Nome)
                .ToListAsync(cancellationToken);
        }

        public async Task<Cliente?> GetActiveCustomerAsync(int clienteId, CancellationToken cancellationToken = default) {
            return await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clienteId && c.IsAtivo, cancellationToken);
        }

        public async Task<CupomDesconto?> GetCouponAsync(int cupomId, CancellationToken cancellationToken = default) {
            return await _context.CuponsDesconto.FirstOrDefaultAsync(c => c.Id == cupomId, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<Dictionary<int, decimal>> CalcularValoresSugeridosAsync(List<Troca> trocas, CancellationToken cancellationToken) {
            var pedidoIds = trocas
                .Select(t => t.PedidoId)
                .Distinct()
                .ToList();

            if (pedidoIds.Count == 0) {
                return new Dictionary<int, decimal>();
            }

            var subtotais = await _context.PedidoItens
                .AsNoTracking()
                .Where(i => pedidoIds.Contains(i.PedidoId))
                .GroupBy(i => i.PedidoId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Sum(i => i.PrecoUnitario * i.Quantidade),
                    cancellationToken);

            var descontos = await _context.CuponsDesconto
                .AsNoTracking()
                .Where(c => c.PedidoId.HasValue && pedidoIds.Contains(c.PedidoId.Value))
                .GroupBy(c => c.PedidoId!.Value)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Sum(c => c.Valor),
                    cancellationToken);

            var valores = new Dictionary<int, decimal>();
            foreach (var troca in trocas) {
                valores[troca.Id] = CalcularValorCupomTroca(troca, subtotais, descontos);
            }

            return valores;
        }

        private static decimal CalcularValorCupomTroca(Troca troca, Dictionary<int, decimal> subtotais, Dictionary<int, decimal> descontos) {
            if (troca.PedidoItem == null || troca.Pedido == null) {
                return 0;
            }

            var subtotalPedido = subtotais.TryGetValue(troca.Pedido.Id, out var subtotal) ? subtotal : 0m;
            var descontoPedido = descontos.TryGetValue(troca.Pedido.Id, out var desconto) ? desconto : 0m;
            var totalItem = troca.PedidoItem.PrecoUnitario * troca.PedidoItem.Quantidade;

            if (subtotalPedido <= 0) {
                return decimal.Round(totalItem, 2);
            }

            var fretePedido = Math.Max(troca.Pedido.Total - subtotalPedido + descontoPedido, 0);
            var proporcaoItem = totalItem / subtotalPedido;
            var freteProporcional = decimal.Round(fretePedido * proporcaoItem, 2);
            return decimal.Round(totalItem + freteProporcional, 2);
        }

        private static IQueryable<Troca> ApplyTradeFilters(IQueryable<Troca> query, AdminExchangesQuery filters) {
            if (!string.IsNullOrWhiteSpace(filters.Busca)) {
                var buscaNormalizada = filters.Busca.Trim();
                query = query.Where(t =>
                    t.Codigo.Contains(buscaNormalizada) ||
                    t.Cliente.Nome.Contains(buscaNormalizada) ||
                    t.PedidoId.ToString().Contains(buscaNormalizada) ||
                    t.PedidoItem.Livro.Titulo.Contains(buscaNormalizada));
            }

            if (!string.IsNullOrWhiteSpace(filters.Status)) {
                if (string.Equals(filters.Status, "TROCA AUTORIZADA", StringComparison.OrdinalIgnoreCase)) {
                    query = query.Where(t =>
                        t.Status == "TROCA AUTORIZADA" ||
                        t.Status == "Autorizada" ||
                        (t.Status == "Aprovado" && !t.CupomDescontoId.HasValue));
                }
                else if (string.Equals(filters.Status, "TROCADO", StringComparison.OrdinalIgnoreCase)) {
                    query = query.Where(t =>
                        t.Status == "TROCADO" ||
                        t.Status == "Recebida" ||
                        (t.Status == "Aprovado" && t.CupomDescontoId.HasValue));
                }
                else if (string.Equals(filters.Status, "EM TROCA", StringComparison.OrdinalIgnoreCase)) {
                    query = query.Where(t => t.Status == "EM TROCA" || t.Status == "Solicitado");
                }
                else if (string.Equals(filters.Status, "TROCA RECUSADA", StringComparison.OrdinalIgnoreCase)) {
                    query = query.Where(t => t.Status == "TROCA RECUSADA" || t.Status == "Recusado");
                }
                else {
                    query = query.Where(t => t.Status == filters.Status);
                }
            }

            return query;
        }
    }
}
