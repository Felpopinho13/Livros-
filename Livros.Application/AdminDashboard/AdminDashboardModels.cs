namespace Livros.Application.AdminDashboard;

public sealed class AdminDashboardResult {
    public DateTime ReferenceDate { get; set; }
    public DateTime PeriodStart { get; set; }
    public int TotalOrders { get; set; }
    public int OrdersInPeriod { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueInPeriod { get; set; }
    public int ActiveCustomers { get; set; }
    public int AdminCustomers { get; set; }
    public int ActiveBooks { get; set; }
    public int CategoriesCount { get; set; }
    public int OpenExchanges { get; set; }
    public int ExchangesAwaitingAnalysis { get; set; }
    public int LowStockBooks { get; set; }
    public int OutOfStockBooks { get; set; }
    public List<AdminDashboardRecentOrder> RecentOrders { get; set; } = new();
    public List<AdminDashboardRecentExchange> RecentExchanges { get; set; } = new();
    public List<AdminDashboardStockAlert> StockAlerts { get; set; } = new();
}

public sealed class AdminDashboardSnapshot {
    public int TotalOrders { get; set; }
    public int OrdersInPeriod { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueInPeriod { get; set; }
    public int ActiveCustomers { get; set; }
    public int AdminCustomers { get; set; }
    public int ActiveBooks { get; set; }
    public int CategoriesCount { get; set; }
    public int OpenExchanges { get; set; }
    public int ExchangesAwaitingAnalysis { get; set; }
    public int LowStockBooks { get; set; }
    public int OutOfStockBooks { get; set; }
    public List<AdminDashboardRecentOrder> RecentOrders { get; set; } = new();
    public List<AdminDashboardRecentExchange> RecentExchanges { get; set; } = new();
    public List<AdminDashboardStockAlert> StockAlerts { get; set; } = new();
}

public sealed class AdminDashboardRecentOrder {
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class AdminDashboardRecentExchange {
    public int ExchangeId { get; set; }
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}

public sealed class AdminDashboardStockAlert {
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int MinimumQuantity { get; set; }
}
