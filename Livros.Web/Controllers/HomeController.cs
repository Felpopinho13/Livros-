using Livros.Application.Catalog;
using Livros.Web.Models;
using Livros.Web.Models.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

public class HomeController : Controller {
    private readonly CatalogService _catalogService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(CatalogService catalogService, ILogger<HomeController> logger) {
        _catalogService = catalogService;
        _logger = logger;
    }

    public IActionResult Index(string? busca) {
        var livros = _catalogService.ListActiveBooks(new CatalogListQuery {
            Busca = busca
        });

        return View(CatalogoViewModelMapper.Map(livros));
    }

    public IActionResult Detalhes(int id) {
        var livro = _catalogService.GetBookDetails(id);

        if (livro == null) {
            return NotFound();
        }

        return View(livro);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(string? requestId = null) {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var resolvedRequestId = requestId ?? Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        if (exceptionFeature?.Error != null) {
            _logger.LogError(
                exceptionFeature.Error,
                "Excecao nao tratada capturada globalmente. RequestId: {RequestId}, Path: {Path}",
                resolvedRequestId,
                exceptionFeature.Path);
        }

        return View("~/Views/Shared/Error.cshtml", new ErrorViewModel {
            RequestId = resolvedRequestId,
            Path = exceptionFeature?.Path
        });
    }
}
