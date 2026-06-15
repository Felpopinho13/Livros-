using Livros.Application.AdminDashboard;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services;

public sealed class AdminDashboardDataProvider : IAdminDashboardDataProvider {
    private static readonly string[] RevenueStatuses = {
        "APROVADA",
        "PAGAMENTO APROVADO",
        "EM SEPARACAO",
        "EM TRANSPORTE",
        "ENVIADO",
        "ENTREGUE"
    };

    private readonly AppDbContext _context;

    public AdminDashboardDataProvider(AppDbContext context) {
        _context = context;
    }

    public async Task<AdminDashboardSnapshot> LoadAsync(DateTime periodStart, int take, CancellationToken cancellationToken = default) {
        var recentOrders = await _context.Pedidos
            .AsNoTracking()
            .Include(order => order.Cliente)
            .OrderByDescending(order => order.Data)
            .Take(take)
            .Select(order => new AdminDashboardRecentOrder {
                OrderId = order.Id,
                CustomerName = order.Cliente != null ? order.Cliente.Nome : "Cliente nao informado",
                Date = order.Data,
                Total = order.Total,
                Status = order.Status
            })
            .ToListAsync(cancellationToken);

        var recentExchanges = await _context.Trocas
            .AsNoTracking()
            .Include(exchange => exchange.Cliente)
            .Include(exchange => exchange.PedidoItem)
                .ThenInclude(item => item.Livro)
            .OrderByDescending(exchange => exchange.DataSolicitacao)
            .Take(take)
            .Select(exchange => new AdminDashboardRecentExchange {
                ExchangeId = exchange.Id,
                OrderId = exchange.PedidoId,
                CustomerName = exchange.Cliente != null ? exchange.Cliente.Nome : "Cliente nao informado",
                BookTitle = exchange.PedidoItem != null && exchange.PedidoItem.Livro != null
                    ? exchange.PedidoItem.Livro.Titulo
                    : "Livro nao informado",
                Status = exchange.Status,
                RequestedAt = exchange.DataSolicitacao
            })
            .ToListAsync(cancellationToken);

        var stockAlerts = await _context.Estoques
            .AsNoTracking()
            .Include(stock => stock.Livro)
            .Where(stock => stock.Livro.IsAtivo && stock.Quantidade <= stock.QuantidadeMinima)
            .OrderBy(stock => stock.Quantidade)
            .ThenBy(stock => stock.Livro.Titulo)
            .Take(take)
            .Select(stock => new AdminDashboardStockAlert {
                BookId = stock.LivroId,
                Title = stock.Livro.Titulo,
                Author = string.IsNullOrWhiteSpace(stock.Livro.Autor) ? "Autor nao informado" : stock.Livro.Autor,
                Quantity = stock.Quantidade,
                MinimumQuantity = stock.QuantidadeMinima
            })
            .ToListAsync(cancellationToken);

        var totalOrders = await _context.Pedidos.CountAsync(cancellationToken);
        var ordersInPeriod = await _context.Pedidos.CountAsync(order => order.Data >= periodStart, cancellationToken);
        var totalRevenue = await _context.Pedidos
            .Where(order => RevenueStatuses.Contains(order.Status))
            .SumAsync(order => (decimal?)order.Total, cancellationToken) ?? 0m;
        var revenueInPeriod = await _context.Pedidos
            .Where(order => order.Data >= periodStart && RevenueStatuses.Contains(order.Status))
            .SumAsync(order => (decimal?)order.Total, cancellationToken) ?? 0m;

        return new AdminDashboardSnapshot {
            TotalOrders = totalOrders,
            OrdersInPeriod = ordersInPeriod,
            TotalRevenue = totalRevenue,
            RevenueInPeriod = revenueInPeriod,
            ActiveCustomers = await _context.Clientes.CountAsync(customer => customer.IsAtivo && !customer.IsAdmin, cancellationToken),
            AdminCustomers = await _context.Clientes.CountAsync(customer => customer.IsAtivo && customer.IsAdmin, cancellationToken),
            ActiveBooks = await _context.Livros.CountAsync(book => book.IsAtivo, cancellationToken),
            CategoriesCount = await _context.Categorias.CountAsync(cancellationToken),
            OpenExchanges = await _context.Trocas.CountAsync(
                exchange => exchange.Status != "TROCADO" && exchange.Status != "TROCA RECUSADA",
                cancellationToken),
            ExchangesAwaitingAnalysis = await _context.Trocas.CountAsync(
                exchange => exchange.Status == "EM TROCA" || exchange.Status == "Solicitado",
                cancellationToken),
            LowStockBooks = await _context.Estoques.CountAsync(
                stock => stock.Livro.IsAtivo && stock.Quantidade <= stock.QuantidadeMinima,
                cancellationToken),
            OutOfStockBooks = await _context.Estoques.CountAsync(
                stock => stock.Livro.IsAtivo && stock.Quantidade <= 0,
                cancellationToken),
            RecentOrders = recentOrders,
            RecentExchanges = recentExchanges,
            StockAlerts = stockAlerts
        };
    }
}
