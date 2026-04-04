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
        // 🔥 VALIDAÇÕES AQUI

        if (_service.EmailExiste(email)) {
            TempData["Erro"] = "Este email já está cadastrado.";
            return RedirectToAction("Cadastro");
        }

        var regex = new System.Text.RegularExpressions.Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[\W_]).{8,}$");

        if (!regex.IsMatch(senha)) {
            TempData["Erro"] = "Senha fraca! Use 8 caracteres com maiúscula, minúscula e símbolo.";
            return RedirectToAction("Cadastro");
        }


        var cliente = new Cliente {
            Nome = nome,
            Email = email,
            Senha = BCrypt.Net.BCrypt.HashPassword(senha),
            CPF = cpf,
            Telefone = telefone,
            Genero = genero,
            DataNascimento = dataNascimento
        };

        var estadoEntity = _context.Estados
            .FirstOrDefault(e => e.Sigla == estado);

        if (estadoEntity == null) {
            estadoEntity = new Estado {
                Nome = estado,
                Sigla = estado
            };
            _context.Estados.Add(estadoEntity);
            _context.SaveChanges();
        }

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