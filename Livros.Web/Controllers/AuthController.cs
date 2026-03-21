using Livros.Domain;
using Livros.Infrastructure.Services;
using Livros.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

public class AuthController : Controller {
    private readonly ClienteService _service;
    private readonly AppDbContext _context;

    public AuthController(ClienteService service, AppDbContext context) {
        _service = service;
        _context = context;
    }

    public IActionResult Cadastro() {
        return View();
    }

    public IActionResult Login() {
        return View();
    }

    [HttpPost]
    public IActionResult Cadastro(
    string nome,
    string email,
    string senha,
    string cpf,
    string telefone,
    string genero,
    DateTime? dataNascimento,

    string nomeEndereco,
    string cep,
    string logradouro,
    string numero,
    string complemento,
    string bairro,
    string cidade,
    string estado
) {
        var cliente = new Cliente {
            Nome = nome,
            Email = email,
            Senha = senha,
            CPF = cpf,
            Telefone = telefone,
            Genero = genero,
            DataNascimento = dataNascimento
        };

        // 🔥 1. Buscar estado
        var estadoEntity = _context.Estados
            .FirstOrDefault(e => e.Sigla == estado);

        if (estadoEntity == null) {
            // segurança (evita erro se não tiver estado no banco)
            estadoEntity = new Estado {
                Nome = estado,
                Sigla = estado
            };
            _context.Estados.Add(estadoEntity);
            _context.SaveChanges();
        }

        // 🔥 2. Buscar ou criar cidade
        var cidadeEntity = _context.Cidades
            .FirstOrDefault(c => c.Nome == cidade && c.EstadoId == estadoEntity.Id);

        if (cidadeEntity == null) {
            cidadeEntity = new Cidade {
                Nome = cidade,
                EstadoId = estadoEntity.Id
            };
            _context.Cidades.Add(cidadeEntity);
            _context.SaveChanges();
        }

        // 🔥 3. Buscar ou criar bairro
        var bairroEntity = _context.Bairros
            .FirstOrDefault(b => b.Nome == bairro && b.CidadeId == cidadeEntity.Id);

        if (bairroEntity == null) {
            bairroEntity = new Bairro {
                Nome = bairro,
                CidadeId = cidadeEntity.Id
            };
            _context.Bairros.Add(bairroEntity);
            _context.SaveChanges();
        }

        // 🔥 4. Criar endereço correto
        var endereco = new Endereco {
            NomeEndereco = nomeEndereco,
            CEP = cep,
            Logradouro = logradouro,
            Numero = numero,
            Complemento = complemento,
            CidadeId = cidadeEntity.Id,
            BairroId = bairroEntity.Id,
            Cliente = cliente
        };

        cliente.Enderecos = new List<Endereco> { endereco };

        _service.Adicionar(cliente);

        return RedirectToAction("Login");
    }

    [HttpPost]
    public IActionResult Login(string email, string senha) {
        var cliente = _service.BuscarPorEmailESenha(email, senha);

        if (cliente == null) {
            ViewBag.Erro = "Email ou senha inválidos";
            return View();
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha)) {
            ViewBag.Erro = "Preencha todos os campos";
            return View();
        }

        HttpContext.Session.SetString("Usuario", cliente.Email);
        HttpContext.Session.SetString("IsAdmin", cliente.IsAdmin.ToString());

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout() {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}