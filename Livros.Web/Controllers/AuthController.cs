using Livros.Application.Authentication;
using Livros.Application.CustomerIdentity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Livros.Web.Services;

public class AuthController : Controller {
    private readonly CustomerIdentityService _customerIdentityService;
    private readonly AuthWorkflowService _authWorkflowService;
    private readonly CartSessionService _cartSessionService;
    private readonly UserSessionService _userSessionService;

    public AuthController(
        CustomerIdentityService customerIdentityService,
        AuthWorkflowService authWorkflowService,
        CartSessionService cartSessionService,
        UserSessionService userSessionService) {
        _customerIdentityService = customerIdentityService;
        _authWorkflowService = authWorkflowService;
        _cartSessionService = cartSessionService;
        _userSessionService = userSessionService;
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

        var loginResult = _customerIdentityService.Authenticate(email, senha);
        if (!loginResult.Authenticated || loginResult.Customer == null) {
            ViewBag.Erro = "Email ou senha invÃ¡lidos";
            return View();
        }

        var cliente = loginResult.Customer;
        var mergeResult = _authWorkflowService.MergeCartOnLogin(new CustomerLoginCartMergeCommand {
            CustomerId = cliente.Id,
            PersistedCartJson = cliente.CarrinhoPersistidoJson,
            CurrentSessionCartJson = _cartSessionService.LoadCartJson(HttpContext.Session),
            SessionKey = _cartSessionService.EnsureReservationSessionKey(HttpContext.Session)
        });

        _userSessionService.SignIn(HttpContext.Session, cliente.Email, cliente.IsAdmin, cliente.Id);

        if (mergeResult.HasItems && !string.IsNullOrWhiteSpace(mergeResult.MergedCartJson)) {
            _cartSessionService.SaveCartJson(HttpContext.Session, mergeResult.MergedCartJson);
            cliente.CarrinhoPersistidoJson = mergeResult.MergedCartJson;
        }
        else {
            _cartSessionService.ClearCart(HttpContext.Session);
        }

        if (!string.IsNullOrEmpty(returnUrl)) {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout() {
        _userSessionService.Clear(HttpContext.Session);
        return RedirectToAction("Index", "Home");
    }
}
