using Microsoft.EntityFrameworkCore;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔥 SERVICES (ANTES DO BUILD)
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=localhost;Database=LivrosDb;Trusted_Connection=True;TrustServerCertificate=True;"));

builder.Services.AddScoped<ClienteService>();

builder.Services.AddSession(); // ✅ AQUI

var app = builder.Build();

// 🔥 MIDDLEWARE

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // ✅ AQUI

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();