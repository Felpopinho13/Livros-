using Livros.Application.AdminDashboard;
using Livros.Application.AdminOrders;

namespace Livros.Web.Models.ViewModels;

public static class AdminDashboardViewModelMapper {
    public static AdminDashboardViewModel Map(AdminDashboardResult dashboard) {
        return new AdminDashboardViewModel {
            ReferenceDate = dashboard.ReferenceDate,
            PeriodStart = dashboard.PeriodStart,
            Metrics = BuildMetrics(dashboard),
            RecentOrders = dashboard.RecentOrders
                .Select(order => new AdminDashboardRecentOrderViewModel {
                    OrderId = order.OrderId,
                    CustomerName = order.CustomerName,
                    Date = order.Date,
                    Total = order.Total,
                    Status = OrderStatusHelper.NormalizeDisplayStatus(order.Status),
                    StatusCssClass = MapOrderStatusCssClass(order.Status)
                })
                .ToList(),
            RecentExchanges = dashboard.RecentExchanges
                .Select(exchange => new AdminDashboardRecentExchangeViewModel {
                    ExchangeId = exchange.ExchangeId,
                    OrderId = exchange.OrderId,
                    CustomerName = exchange.CustomerName,
                    BookTitle = exchange.BookTitle,
                    Status = NormalizeExchangeStatus(exchange.Status),
                    StatusCssClass = MapExchangeStatusCssClass(exchange.Status),
                    RequestedAt = exchange.RequestedAt
                })
                .ToList(),
            StockAlerts = dashboard.StockAlerts
                .Select(stock => new AdminDashboardStockAlertViewModel {
                    BookId = stock.BookId,
                    Title = stock.Title,
                    Author = stock.Author,
                    Quantity = stock.Quantity,
                    MinimumQuantity = stock.MinimumQuantity,
                    Status = stock.Quantity <= 0 ? "Sem estoque" : "Abaixo do minimo",
                    StatusCssClass = stock.Quantity <= 0 ? "danger" : "warning"
                })
                .ToList()
        };
    }

    private static List<AdminDashboardMetricCardViewModel> BuildMetrics(AdminDashboardResult dashboard) {
        return new List<AdminDashboardMetricCardViewModel> {
            new() {
                Title = "Total de Pedidos",
                Value = dashboard.TotalOrders.ToString("N0"),
                Info = $"{dashboard.OrdersInPeriod:N0} nos ultimos 30 dias",
                InfoCssClass = "card-info"
            },
            new() {
                Title = "Faturamento",
                Value = dashboard.TotalRevenue.ToString("C"),
                Info = $"{dashboard.RevenueInPeriod.ToString("C")} nos ultimos 30 dias",
                InfoCssClass = "card-info"
            },
            new() {
                Title = "Clientes Ativos",
                Value = dashboard.ActiveCustomers.ToString("N0"),
                Info = $"{dashboard.AdminCustomers:N0} administradores ativos",
                InfoCssClass = "card-info"
            },
            new() {
                Title = "Catalogo",
                Value = dashboard.ActiveBooks.ToString("N0"),
                Info = $"{dashboard.CategoriesCount:N0} categorias cadastradas",
                InfoCssClass = "card-info"
            },
            new() {
                Title = "Trocas Abertas",
                Value = dashboard.OpenExchanges.ToString("N0"),
                Info = $"{dashboard.ExchangesAwaitingAnalysis:N0} aguardando analise",
                InfoCssClass = dashboard.ExchangesAwaitingAnalysis > 0 ? "card-info warning" : "card-info"
            },
            new() {
                Title = "Estoque em Alerta",
                Value = dashboard.LowStockBooks.ToString("N0"),
                Info = $"{dashboard.OutOfStockBooks:N0} sem estoque",
                InfoCssClass = dashboard.OutOfStockBooks > 0 ? "card-info warning" : "card-info"
            }
        };
    }

    private static string MapOrderStatusCssClass(string? status) {
        return OrderStatusHelper.NormalizeInternalStatus(status) switch {
            "ENTREGUE" => "success",
            "CANCELADO" => "danger",
            "REPROVADA" => "danger",
            _ => "pending"
        };
    }

    private static string NormalizeExchangeStatus(string? status) {
        var normalized = (status ?? string.Empty).Trim().ToUpperInvariant();
        return normalized switch {
            "SOLICITADO" => "EM TROCA",
            "AUTORIZADA" => "TROCA AUTORIZADA",
            "RECUSADO" => "TROCA RECUSADA",
            "RECEBIDA" => "TROCADO",
            _ => string.IsNullOrWhiteSpace(status) ? "NAO INFORMADO" : status
        };
    }

    private static string MapExchangeStatusCssClass(string? status) {
        var normalized = NormalizeExchangeStatus(status);
        return normalized switch {
            "TROCADO" => "success",
            "TROCA RECUSADA" => "danger",
            _ => "pending"
        };
    }
}
