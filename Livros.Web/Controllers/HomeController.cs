using Livros.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller {
    private readonly CatalogService _catalogService;

    public HomeController(CatalogService catalogService) {
        _catalogService = catalogService;
    }

    public IActionResult Index() {
        var livros = _catalogService.ListActiveBooks();
        return View(livros);
    }

    public IActionResult Detalhes(int id) {
        var livro = _catalogService.GetBookDetails(id);

        if (livro == null) {
            return NotFound();
        }

        return View(livro);
    }
}
