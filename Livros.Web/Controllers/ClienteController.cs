using Microsoft.AspNetCore.Mvc;
using Livros.Infrastructure.Services;

public class ClienteController : Controller {
    private readonly ClienteService _service;

    public ClienteController(ClienteService service) {
        _service = service;
    }

    public IActionResult Index() {
        var clientes = _service.Listar();
        return View(clientes);
    }
}