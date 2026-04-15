using Livros.Domain;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var culture = new CultureInfo("pt-BR");

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=localhost;Database=LivrosDb;Trusted_Connection=True;TrustServerCertificate=True;"));

builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<LivroService>();
builder.Services.AddScoped<EnderecoService>();
builder.Services.AddScoped<EstoqueService>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.Categorias.Any()) {
        context.Categorias.AddRange(
            new Categoria { Nome = "Romance" },
            new Categoria { Nome = "Ficcao" },
            new Categoria { Nome = "Fantasia" },
            new Categoria { Nome = "Drama" },
            new Categoria { Nome = "Biografia" },
            new Categoria { Nome = "Negocios" },
            new Categoria { Nome = "Tecnologia" },
            new Categoria { Nome = "Classicos" }
        );
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
