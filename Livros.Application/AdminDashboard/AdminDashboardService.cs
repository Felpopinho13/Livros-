namespace Livros.Application.AdminDashboard;

public sealed class AdminDashboardService {
    private readonly IAdminDashboardDataProvider _dataProvider;

    public AdminDashboardService(IAdminDashboardDataProvider dataProvider) {
        _dataProvider = dataProvider;
    }

    public async Task<AdminDashboardResult> BuildAsync(CancellationToken cancellationToken = default) {
        var referenceDate = DateTime.Today;
        var periodStart = referenceDate.AddDays(-30);
        var snapshot = await _dataProvider.LoadAsync(periodStart, 5, cancellationToken);

        return new AdminDashboardResult {
            ReferenceDate = referenceDate,
            PeriodStart = periodStart,
            TotalOrders = snapshot.TotalOrders,
            OrdersInPeriod = snapshot.OrdersInPeriod,
            TotalRevenue = snapshot.TotalRevenue,
            RevenueInPeriod = snapshot.RevenueInPeriod,
            ActiveCustomers = snapshot.ActiveCustomers,
            AdminCustomers = snapshot.AdminCustomers,
            ActiveBooks = snapshot.ActiveBooks,
            CategoriesCount = snapshot.CategoriesCount,
            OpenExchanges = snapshot.OpenExchanges,
            ExchangesAwaitingAnalysis = snapshot.ExchangesAwaitingAnalysis,
            LowStockBooks = snapshot.LowStockBooks,
            OutOfStockBooks = snapshot.OutOfStockBooks,
            RecentOrders = snapshot.RecentOrders,
            RecentExchanges = snapshot.RecentExchanges,
            StockAlerts = snapshot.StockAlerts
        };
    }
}
