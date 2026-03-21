using Livros.Domain;
using Livros.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class ClienteController : Controller {
    private readonly ClienteService _service;

    public ClienteController(ClienteService service) {
        _service = service;
    }

    public IActionResult Index() {
        var clientes = _service.Listar();
        return View(clientes);
    }

    public IActionResult AreaCliente() {
        var usuario = HttpContext.Session.GetString("Usuario");

        if (usuario == null) {
            return RedirectToAction("Login", "Auth");
        }

        ViewBag.Usuario = usuario;

        return View();
    }

    public IActionResult Editar() {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var cliente = _service.BuscarPorEmail(email);

        return View(cliente);
    }

    [HttpPost]
    public IActionResult Editar(Cliente cliente) {
        _service.Atualizar(cliente);

        TempData["Sucesso"] = "Dados atualizados com sucesso!";

        return RedirectToAction("Editar");
    }
}
