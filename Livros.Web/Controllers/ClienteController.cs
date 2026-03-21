using Livros.Domain;
using Livros.Infrastructure.Services;
using Livros.Infrastructure.Data;
using Livros.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ClienteController : Controller {
    private readonly ClienteService _service;
    private readonly AppDbContext _context;

    public ClienteController(ClienteService service, AppDbContext context) {
        _service = service;
        _context = context;
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

    public IActionResult EditarEndereco(int id) {
        var endereco = _context.Enderecos
            .Include(e => e.Bairro)
                .ThenInclude(b => b.Cidade)
            .Include(e => e.Cidade)
                .ThenInclude(c => c.Estado)
            .FirstOrDefault(e => e.Id == id);

        if (endereco == null)
            return NotFound();

        var vm = new EnderecoViewModel {
            Id = endereco.Id,
            NomeEndereco = endereco.NomeEndereco,
            CEP = endereco.CEP,
            Logradouro = endereco.Logradouro,
            Numero = endereco.Numero,
            Complemento = endereco.Complemento,
            Bairro = endereco.Bairro.Nome,
            Cidade = endereco.Cidade.Nome,
            Estado = endereco.Cidade.Estado.Sigla
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult EditarEndereco(EnderecoViewModel vm) {
        var endereco = _context.Enderecos
            .Include(e => e.Cidade)
            .Include(e => e.Bairro)
            .FirstOrDefault(e => e.Id == vm.Id);

        if (endereco == null)
            return NotFound();

        // Atualiza dados básicos
        endereco.NomeEndereco = vm.NomeEndereco;
        endereco.CEP = vm.CEP;
        endereco.Logradouro = vm.Logradouro;
        endereco.Numero = vm.Numero;
        endereco.Complemento = vm.Complemento;

        // 🔥 Atualiza cidade/bairro (mesma lógica do cadastro)
        var estado = _context.Estados.FirstOrDefault(e => e.Sigla == vm.Estado);

        var cidade = _context.Cidades
            .FirstOrDefault(c => c.Nome == vm.Cidade && c.EstadoId == estado.Id);

        if (cidade == null) {
            cidade = new Cidade { Nome = vm.Cidade, EstadoId = estado.Id };
            _context.Cidades.Add(cidade);
            _context.SaveChanges();
        }

        var bairro = _context.Bairros
            .FirstOrDefault(b => b.Nome == vm.Bairro && b.CidadeId == cidade.Id);

        if (bairro == null) {
            bairro = new Bairro { Nome = vm.Bairro, CidadeId = cidade.Id };
            _context.Bairros.Add(bairro);
            _context.SaveChanges();
        }

        endereco.CidadeId = cidade.Id;
        endereco.BairroId = bairro.Id;

        _context.SaveChanges();

        TempData["Sucesso"] = "Endereço atualizado com sucesso!";

        return RedirectToAction("EditarEndereco", new { id = vm.Id });
    }

    public IActionResult Enderecos() {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var cliente = _context.Clientes
            .Include(c => c.Enderecos)
                .ThenInclude(e => e.Bairro)
                    .ThenInclude(b => b.Cidade)
            .Include(c => c.Enderecos)
                .ThenInclude(e => e.Cidade)
                    .ThenInclude(c => c.Estado)
            .FirstOrDefault(c => c.Email == email);

        return View(cliente.Enderecos.ToList());
    }

    [HttpPost]
    public IActionResult CadastrarEndereco(
    string nomeEndereco,
    string cep,
    string logradouro,
    string numero,
    string complemento,
    string bairro,
    string cidade,
    string estado) {
        var email = HttpContext.Session.GetString("Usuario");

        var cliente = _context.Clientes.FirstOrDefault(c => c.Email == email);

        // 🔥 Estado
        var estadoEntity = _context.Estados.FirstOrDefault(e => e.Sigla == estado);

        if (estadoEntity == null) {
            estadoEntity = new Estado { Nome = estado, Sigla = estado };
            _context.Estados.Add(estadoEntity);
            _context.SaveChanges();
        }

        // 🔥 Cidade
        var cidadeEntity = _context.Cidades
            .FirstOrDefault(c => c.Nome == cidade && c.EstadoId == estadoEntity.Id);

        if (cidadeEntity == null) {
            cidadeEntity = new Cidade { Nome = cidade, EstadoId = estadoEntity.Id };
            _context.Cidades.Add(cidadeEntity);
            _context.SaveChanges();
        }

        // 🔥 Bairro
        var bairroEntity = _context.Bairros
            .FirstOrDefault(b => b.Nome == bairro && b.CidadeId == cidadeEntity.Id);

        if (bairroEntity == null) {
            bairroEntity = new Bairro { Nome = bairro, CidadeId = cidadeEntity.Id };
            _context.Bairros.Add(bairroEntity);
            _context.SaveChanges();
        }

        // 🔥 Endereço
        var endereco = new Endereco {
            NomeEndereco = nomeEndereco,
            CEP = cep,
            Logradouro = logradouro,
            Numero = numero,
            Complemento = complemento,
            CidadeId = cidadeEntity.Id,
            BairroId = bairroEntity.Id,
            ClienteId = cliente.Id
        };

        _context.Enderecos.Add(endereco);
        _context.SaveChanges();

        TempData["Sucesso"] = "Endereço cadastrado com sucesso!";

        return RedirectToAction("Enderecos");
    }

    public IActionResult TornarPadrao(int id) {
        var email = HttpContext.Session.GetString("Usuario");

        var cliente = _context.Clientes
            .Include(c => c.Enderecos)
            .FirstOrDefault(c => c.Email == email);

        if (cliente == null)
            return RedirectToAction("Login", "Auth");

        // 🔥 remove padrão atual
        foreach (var e in cliente.Enderecos) {
            e.IsPadrao = false;
        }

        // 🔥 define novo padrão
        var endereco = cliente.Enderecos.FirstOrDefault(e => e.Id == id);

        if (endereco != null) {
            endereco.IsPadrao = true;
        }

        _context.SaveChanges();

        return RedirectToAction("Enderecos");
    }
}
