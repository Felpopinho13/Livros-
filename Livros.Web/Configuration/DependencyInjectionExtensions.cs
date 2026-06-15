using Livros.Application.AdminBooks;
using Livros.Application.AdminCustomers;
using Livros.Application.AdminDashboard;
using Livros.Application.AdminExchanges;
using Livros.Application.AdminInventory;
using Livros.Application.AdminOrders;
using Livros.Application.AdminSalesHistory;
using Livros.Application.Common.Logging;
using Livros.Application.Authentication;
using Livros.Application.Catalog;
using Livros.Application.Checkout;
using Livros.Application.CustomerAccounts;
using Livros.Application.CustomerAddresses;
using Livros.Application.CustomerCards;
using Livros.Application.CustomerCart;
using Livros.Application.CustomerCheckout;
using Livros.Application.CustomerIdentity;
using Livros.Application.CustomerOrders;
using Livros.Application.Recommendations;
using Livros.Application.SalesAnalysis;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Logging;
using Livros.Infrastructure.Services;
using Livros.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Livros.Web.Configuration;

public static class DependencyInjectionExtensions {
    private const string DefaultConnectionString = "Server=localhost;Database=LivrosDb;Trusted_Connection=True;TrustServerCertificate=True;";

    public static IServiceCollection AddLivrosPlatformServices(this IServiceCollection services) {
        services.AddControllersWithViews();
        services.AddDistributedMemoryCache();
        services.AddSession();
        return services;
    }

    public static IServiceCollection AddLivrosPersistence(this IServiceCollection services, IConfiguration configuration) {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? DefaultConnectionString;

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }

    public static IServiceCollection AddLivrosFeatureServices(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<LivroRecommendationAiOptions>(configuration.GetSection("OpenAI"));
        services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));

        services.AddScoped<IAuthWorkflowDataProvider, AuthWorkflowDataProvider>();
        services.AddScoped<AuthWorkflowService>();
        services.AddScoped<ICatalogDataProvider, CatalogDataProvider>();
        services.AddScoped<CatalogService>();
        services.AddScoped<ICustomerIdentityDataProvider, CustomerIdentityDataProvider>();
        services.AddScoped<CustomerIdentityService>();
        services.AddScoped<ICustomerAddressDataProvider, CustomerAddressDataProvider>();
        services.AddScoped<CustomerAddressService>();
        services.AddScoped<ICustomerAccountDataProvider, CustomerAccountDataProvider>();
        services.AddScoped<CustomerAccountService>();
        services.AddScoped<ICustomerCardDataProvider, CustomerCardDataProvider>();
        services.AddScoped<CustomerCardService>();
        services.AddScoped<ICustomerCartDataProvider, CustomerCartDataProvider>();
        services.AddScoped<CustomerCartService>();
        services.AddScoped<ICustomerCheckoutDataProvider, CustomerCheckoutDataProvider>();
        services.AddScoped<CustomerCheckoutService>();
        services.AddScoped<ICustomerOrderPlacementDataProvider, CustomerOrderPlacementDataProvider>();
        services.AddScoped<CustomerOrderPlacementService>();
        services.AddScoped<ICustomerOrdersDataProvider, CustomerOrdersDataProvider>();
        services.AddScoped<CustomerOrdersService>();
        services.AddScoped<IAdminCustomersDataProvider, AdminCustomersDataProvider>();
        services.AddScoped<AdminCustomersService>();
        services.AddScoped<IAdminDashboardDataProvider, AdminDashboardDataProvider>();
        services.AddScoped<AdminDashboardService>();
        services.AddScoped<IAdminBooksDataProvider, AdminBooksDataProvider>();
        services.AddScoped<AdminBooksService>();
        services.AddScoped<IAdminSalesHistorySeedDataProvider, AdminSalesHistorySeedDataProvider>();
        services.AddScoped<AdminSalesHistorySeedService>();
        services.AddScoped<IAdminExchangesDataProvider, AdminExchangesDataProvider>();
        services.AddScoped<AdminExchangesService>();
        services.AddScoped<IAdminOrdersDataProvider, AdminOrdersDataProvider>();
        services.AddScoped<AdminOrdersService>();
        services.AddScoped<IAdminInventoryDataProvider, AdminInventoryDataProvider>();
        services.AddScoped<AdminInventoryService>();
        services.AddScoped<ICheckoutPricingDataProvider, CheckoutPricingDataProvider>();
        services.AddScoped<CheckoutPricingService>();
        services.AddScoped<ICheckoutAddressDataProvider, CheckoutAddressDataProvider>();
        services.AddScoped<CheckoutAddressService>();
        services.AddScoped<CheckoutOrderService>();
        services.AddScoped<ICheckoutPaymentDataProvider, CheckoutPaymentDataProvider>();
        services.AddScoped<CheckoutPaymentService>();
        services.AddScoped<ILivroRecommendationDataProvider, LivroRecommendationDataProvider>();
        services.AddScoped<LivroRecommendationChatService>();
        services.AddScoped<ISalesAnalysisDataProvider, SalesAnalysisDataProvider>();
        services.AddScoped<SalesAnalysisService>();
        services.AddScoped<AppBootstrapService>();
        services.AddScoped<BookImageStorageService>();
        services.AddScoped<CartSessionService>();
        services.AddScoped<ChatbotSessionService>();
        services.AddScoped<UserSessionService>();

        services.AddHttpClient<ILivroRecommendationAiClient, LivroRecommendationOpenAiClient>(client => {
            client.Timeout = TimeSpan.FromSeconds(25);
        });

        return services;
    }
}
