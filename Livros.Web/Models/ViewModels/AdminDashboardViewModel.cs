namespace Livros.Web.Models.ViewModels;

public sealed class AdminDashboardViewModel {
    public DateTime ReferenceDate { get; set; }
    public DateTime PeriodStart { get; set; }
    public List<AdminDashboardMetricCardViewModel> Metrics { get; set; } = new();
    public List<AdminDashboardRecentOrderViewModel> RecentOrders { get; set; } = new();
    public List<AdminDashboardRecentExchangeViewModel> RecentExchanges { get; set; } = new();
    public List<AdminDashboardStockAlertViewModel> StockAlerts { get; set; } = new();
}

public sealed class AdminDashboardMetricCardViewModel {
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Info { get; set; } = string.Empty;
    public string InfoCssClass { get; set; } = "card-info";
}

public sealed class AdminDashboardRecentOrderViewModel {
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = "pending";
}

public sealed class AdminDashboardRecentExchangeViewModel {
    public int ExchangeId { get; set; }
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = "pending";
    public DateTime RequestedAt { get; set; }
}

public sealed class AdminDashboardStockAlertViewModel {
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int MinimumQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = "warning";
}
