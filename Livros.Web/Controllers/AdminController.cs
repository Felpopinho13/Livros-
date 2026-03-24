using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class AdminController : Controller {
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context) {
        _context = context;
    }

    public IActionResult Dashboard() {
        return View();
    }

    public IActionResult Clientes(string busca, string status, string admin, int pagina = 1) {
        int pageSize = 10;

        var query = _context.Clientes.AsQueryable();

        if (!string.IsNullOrEmpty(busca)) {
            query = query.Where(c =>
                c.Nome.Contains(busca) ||
                c.Email.Contains(busca));
        }

        if (!string.IsNullOrEmpty(status)) {
            if (status == "ativo")
                query = query.Where(c => c.IsAtivo);
            else if (status == "inativo")
                query = query.Where(c => !c.IsAtivo);
        }

        if (!string.IsNullOrEmpty(admin)) {
            bool isAdmin = bool.Parse(admin);
            query = query.Where(c => c.IsAdmin == isAdmin);
        }

        var totalClientes = query.Count();

        var clientes = query
            .OrderBy(c => c.Id)
            .Skip((pagina - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.PaginaAtual = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalClientes / pageSize);

        return View(clientes);
    }

    [HttpPost]
    public IActionResult DesativarCliente(int id) {
        var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);

        if (cliente != null) {
            cliente.IsAtivo = false;
            _context.SaveChanges();
        }

        return RedirectToAction("Clientes");
    }

    [HttpPost]
    public IActionResult AtivarCliente(int id) {
        var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);

        if (cliente != null) {
            cliente.IsAtivo = true;
            _context.SaveChanges();
        }

        return RedirectToAction("Clientes");
    }

    [HttpPost]
    public IActionResult EditarClienteAdmin(Cliente cliente) {
        var clienteDb = _context.Clientes.FirstOrDefault(c => c.Id == cliente.Id);

        if (clienteDb == null)
            return NotFound();

        clienteDb.Nome = cliente.Nome;
        clienteDb.Email = cliente.Email;
        clienteDb.CPF = cliente.CPF;
        clienteDb.Telefone = cliente.Telefone;
        clienteDb.Genero = cliente.Genero;
        clienteDb.DataNascimento = cliente.DataNascimento;
        clienteDb.IsAdmin = cliente.IsAdmin;

        _context.SaveChanges();

        return RedirectToAction("Clientes");
    }

    [HttpPost]
    public IActionResult ExcluirClienteAdmin(int id) {
        var cliente = _context.Clientes
            .Include(c => c.Enderecos)
            .Include(c => c.Cartoes)
            .FirstOrDefault(c => c.Id == id);

        if (cliente == null)
            return NotFound();

        if (cliente.Enderecos != null)
            _context.Enderecos.RemoveRange(cliente.Enderecos);

        if (cliente.Cartoes != null)
            _context.Cartoes.RemoveRange(cliente.Cartoes);

        _context.Clientes.Remove(cliente);

        _context.SaveChanges();

        return RedirectToAction("Clientes");
    }
}