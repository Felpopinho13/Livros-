using Livros.Domain;
using Livros.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

public class AuthController : Controller {
    private readonly ClienteService _service;

    public AuthController(ClienteService service) {
        _service = service;
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

        var endereco = new Endereco {
            NomeEndereco = nomeEndereco,
            CEP = cep,
            Logradouro = logradouro,
            Numero = numero,
            Complemento = complemento,
            Bairro = bairro,
            Cidade = cidade,
            Estado = estado,
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

        return RedirectToAction("Index", "Home");
    }
}