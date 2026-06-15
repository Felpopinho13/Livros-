using Livros.Infrastructure.Services;
using Livros.Web.Configuration;
using System.Globalization;

var culture = new CultureInfo("pt-BR");

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLivrosPlatformServices()
    .AddLivrosPersistence(builder.Configuration)
    .AddLivrosFeatureServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var bootstrapService = scope.ServiceProvider.GetRequiredService<AppBootstrapService>();
    bootstrapService.Initialize();
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
