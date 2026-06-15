using Livros.Application.Authentication;
using Livros.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class AuthController : Controller {
    private const string CarrinhoSessionKey = "Carrinho";

    private readonly ClienteService _service;
    private readonly AuthWorkflowService _authWorkflowService;

    public AuthController(ClienteService service, AuthWorkflowService authWorkflowService) {
        _service = service;
        _authWorkflowService = authWorkflowService;
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
        string tipoLogradouro,
        string logradouro,
        string numero,
        string complemento,
        string tipoResidencia,
        string pais,
        string bairro,
        string cidade,
        string estado) {
        var result = _authWorkflowService.Register(new CustomerRegistrationCommand {
            Nome = nome,
            Email = email,
            Senha = senha,
            CPF = cpf,
            Telefone = telefone,
            Genero = genero,
            DataNascimento = dataNascimento,
            NomeEndereco = nomeEndereco,
            CEP = cep,
            TipoLogradouro = tipoLogradouro,
            Logradouro = logradouro,
            Numero = numero,
            Complemento = complemento,
            TipoResidencia = tipoResidencia,
            Pais = pais,
            Bairro = bairro,
            Cidade = cidade,
            Estado = estado
        });

        if (!result.Success) {
            TempData["Erro"] = result.ErrorMessage;
            return RedirectToAction("Cadastro");
        }

        return RedirectToAction("Login");
    }

    [HttpPost]
    public IActionResult Login(string email, string senha, string returnUrl) {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha)) {
            ViewBag.Erro = "Preencha todos os campos";
            return View();
        }

        var cliente = _service.BuscarPorEmailESenha(email, senha);
        if (cliente == null) {
            ViewBag.Erro = "Email ou senha inválidos";
            return View();
        }

        var mergeResult = _authWorkflowService.MergeCartOnLogin(new CustomerLoginCartMergeCommand {
            CustomerId = cliente.Id,
            PersistedCartJson = cliente.CarrinhoPersistidoJson,
            CurrentSessionCartJson = HttpContext.Session.GetString(CarrinhoSessionKey),
            SessionKey = HttpContext.Session.Id
        });

        HttpContext.Session.SetString("Usuario", cliente.Email);
        HttpContext.Session.SetString("IsAdmin", cliente.IsAdmin.ToString());
        HttpContext.Session.SetString("ClienteId", cliente.Id.ToString());

        if (mergeResult.HasItems && !string.IsNullOrWhiteSpace(mergeResult.MergedCartJson)) {
            HttpContext.Session.SetString(CarrinhoSessionKey, mergeResult.MergedCartJson);
            cliente.CarrinhoPersistidoJson = mergeResult.MergedCartJson;
        }
        else {
            HttpContext.Session.Remove(CarrinhoSessionKey);
        }

        if (!string.IsNullOrEmpty(returnUrl)) {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout() {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
