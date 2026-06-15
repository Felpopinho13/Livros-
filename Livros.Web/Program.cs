using Livros.Domain;
using Livros.Application.AdminExchanges;
using Livros.Application.AdminCustomers;
using Livros.Application.AdminOrders;
using Livros.Application.AdminBooks;
using Livros.Application.Authentication;
using Livros.Application.SalesAnalysis;
using Livros.Application.Checkout;
using Livros.Application.CustomerAddresses;
using Livros.Application.CustomerAccounts;
using Livros.Application.CustomerCards;
using Livros.Application.CustomerCart;
using Livros.Application.CustomerCheckout;
using Livros.Application.CustomerOrders;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Services;
using Livros.Application.Recommendations;
using Livros.Web.Configuration;
using Livros.Web.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

var culture = new CultureInfo("pt-BR");

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=localhost;Database=LivrosDb;Trusted_Connection=True;TrustServerCertificate=True;"));

builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<IAuthWorkflowDataProvider, AuthWorkflowDataProvider>();
builder.Services.AddScoped<AuthWorkflowService>();
builder.Services.AddScoped<LivroService>();
builder.Services.AddScoped<EnderecoService>();
builder.Services.AddScoped<ICustomerAddressDataProvider, CustomerAddressDataProvider>();
builder.Services.AddScoped<CustomerAddressService>();
builder.Services.AddScoped<ICustomerAccountDataProvider, CustomerAccountDataProvider>();
builder.Services.AddScoped<CustomerAccountService>();
builder.Services.AddScoped<ICustomerCardDataProvider, CustomerCardDataProvider>();
builder.Services.AddScoped<CustomerCardService>();
builder.Services.AddScoped<ICustomerCartDataProvider, CustomerCartDataProvider>();
builder.Services.AddScoped<CustomerCartService>();
builder.Services.AddScoped<ICustomerCheckoutDataProvider, CustomerCheckoutDataProvider>();
builder.Services.AddScoped<CustomerCheckoutService>();
builder.Services.AddScoped<ICustomerOrderPlacementDataProvider, CustomerOrderPlacementDataProvider>();
builder.Services.AddScoped<CustomerOrderPlacementService>();
builder.Services.AddScoped<ICustomerOrdersDataProvider, CustomerOrdersDataProvider>();
builder.Services.AddScoped<CustomerOrdersService>();
builder.Services.AddScoped<IAdminCustomersDataProvider, AdminCustomersDataProvider>();
builder.Services.AddScoped<AdminCustomersService>();
builder.Services.AddScoped<IAdminBooksDataProvider, AdminBooksDataProvider>();
builder.Services.AddScoped<AdminBooksService>();
builder.Services.AddScoped<IAdminExchangesDataProvider, AdminExchangesDataProvider>();
builder.Services.AddScoped<AdminExchangesService>();
builder.Services.AddScoped<IAdminOrdersDataProvider, AdminOrdersDataProvider>();
builder.Services.AddScoped<AdminOrdersService>();
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<ICheckoutPricingDataProvider, CheckoutPricingDataProvider>();
builder.Services.AddScoped<CheckoutPricingService>();
builder.Services.AddScoped<ICheckoutAddressDataProvider, CheckoutAddressDataProvider>();
builder.Services.AddScoped<CheckoutAddressService>();
builder.Services.AddScoped<CheckoutOrderService>();
builder.Services.AddScoped<ICheckoutPaymentDataProvider, CheckoutPaymentDataProvider>();
builder.Services.AddScoped<CheckoutPaymentService>();
builder.Services.AddScoped<ILivroRecommendationDataProvider, LivroRecommendationDataProvider>();
builder.Services.AddScoped<ISalesAnalysisDataProvider, SalesAnalysisDataProvider>();
builder.Services.AddScoped<SalesAnalysisService>();
builder.Services.AddScoped<AdminSalesHistorySeedService>();
builder.Services.AddHttpClient<LivroRecommendationChatService>(client => {
    client.Timeout = TimeSpan.FromSeconds(25);
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var categoriasExistentes = context.Categorias.ToList();
    var houveAtualizacaoCategorias = false;

    foreach (var categoriaPadrao in CategoriaCatalogo.Itens) {
        var categoriaExistente = categoriasExistentes.FirstOrDefault(c =>
            NormalizarCategoria(c.Nome) == NormalizarCategoria(categoriaPadrao.Nome));

        if (categoriaExistente == null) {
            context.Categorias.Add(new Categoria {
                Nome = categoriaPadrao.Nome
            });
            houveAtualizacaoCategorias = true;
            continue;
        }

        if (!string.Equals(categoriaExistente.Nome, categoriaPadrao.Nome, StringComparison.Ordinal)) {
            categoriaExistente.Nome = categoriaPadrao.Nome;
            houveAtualizacaoCategorias = true;
        }
    }

    if (houveAtualizacaoCategorias) {
        context.SaveChanges();
    }

    if (!context.Clientes.Any(c => c.Email == "admin@admin.com")) {
        var admin = new Cliente {
            Nome = "Admin",
            Email = "admin@admin.com",
            Senha = BCrypt.Net.BCrypt.HashPassword("123456"),
            IsAdmin = true,
            IsAtivo = true
        };

        context.Clientes.Add(admin);
        context.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); 

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string NormalizarCategoria(string? nome) {
    if (string.IsNullOrWhiteSpace(nome)) {
        return string.Empty;
    }

    var textoNormalizado = nome.Trim().Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder();

    foreach (var caractere in textoNormalizado) {
        var categoriaUnicode = CharUnicodeInfo.GetUnicodeCategory(caractere);
        if (categoriaUnicode != UnicodeCategory.NonSpacingMark) {
            builder.Append(char.ToLowerInvariant(caractere));
        }
    }

    return builder
        .ToString()
        .Normalize(NormalizationForm.FormC);
}









