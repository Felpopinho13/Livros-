using Livros.Domain;
using Livros.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller {
    private readonly LivroService _livroService;

    public HomeController(LivroService livroService) {
        _livroService = livroService;
    }

    public IActionResult Index() {
        var livros = _livroService.Listar();
        return View(livros);
    }

    public IActionResult Detalhes(int id) {
        var livro = _livroService.ObterPorId(id);

        if (livro == null)
            return NotFound();

        return View(livro);
    }

}