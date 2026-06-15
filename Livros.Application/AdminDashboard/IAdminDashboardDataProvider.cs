namespace Livros.Application.AdminDashboard;

public interface IAdminDashboardDataProvider {
    Task<AdminDashboardSnapshot> LoadAsync(DateTime periodStart, int take, CancellationToken cancellationToken = default);
}
