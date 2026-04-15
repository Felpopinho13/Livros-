using Livros.Domain;
using Livros.Infrastructure.Services;
using Livros.Infrastructure.Data;
using Livros.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

public class AuthController : Controller {
    private const string CarrinhoSessionKey = "Carrinho";
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
    string tipoLogradouro,
    string logradouro,
    string numero,
    string complemento,
    string tipoResidencia,
    string pais,
    string bairro,
    string cidade,
    string estado
) {
        // 🔥 VALIDAÇÕES AQUI

        if (_service.EmailExiste(email)) {
            TempData["Erro"] = "Este email já está cadastrado.";
            return RedirectToAction("Cadastro");
        }

        if (!PasswordPolicyHelper.IsStrongPassword(senha)) {
            TempData["Erro"] = PasswordPolicyHelper.MensagemRequisito;
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
            NomeEndereco = nomeEndereco.Trim(),
            CEP = cep.Trim(),
            TipoLogradouro = string.IsNullOrWhiteSpace(tipoLogradouro) ? "Rua" : tipoLogradouro.Trim(),
            Logradouro = logradouro.Trim(),
            Numero = numero.Trim(),
            Complemento = string.IsNullOrWhiteSpace(complemento) ? null : complemento.Trim(),
            TipoResidencia = string.IsNullOrWhiteSpace(tipoResidencia) ? "Casa" : tipoResidencia.Trim(),
            Pais = string.IsNullOrWhiteSpace(pais) ? "Brasil" : pais.Trim(),
            CidadeId = cidadeEntity.Id,
            BairroId = bairroEntity.Id,
            Cliente = cliente,
            IsEntrega = true,
            IsCobranca = true,
            IsPadrao = true
        };

        cliente.Enderecos = new List<Endereco> { endereco };

        _service.Adicionar(cliente);

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

        var sessionKey = HttpContext.Session.Id;
        var carrinhoSessaoAtual = ObterCarrinhoDaSessaoAtual();
        var carrinhoPersistido = DeserializarCarrinho(cliente.CarrinhoPersistidoJson);
        var carrinhoMesclado = MesclarCarrinhos(carrinhoPersistido, carrinhoSessaoAtual);

        HttpContext.Session.SetString("Usuario", cliente.Email);
        HttpContext.Session.SetString("IsAdmin", cliente.IsAdmin.ToString());
        HttpContext.Session.SetString("ClienteId", cliente.Id.ToString()); // 🔥 IMPORTANTE

        if (carrinhoMesclado.Any()) {
            var carrinhoJson = JsonSerializer.Serialize(carrinhoMesclado);
            HttpContext.Session.SetString(CarrinhoSessionKey, carrinhoJson);
            cliente.CarrinhoPersistidoJson = carrinhoJson;
            TransferirReservasSessaoParaCliente(cliente.Id, sessionKey);
            _context.SaveChanges();
        }
        else {
            HttpContext.Session.Remove(CarrinhoSessionKey);
        }

        // 🔥 REDIRECIONAMENTO INTELIGENTE
        if (!string.IsNullOrEmpty(returnUrl)) {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout() {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    private List<CarrinhoSessionItem> ObterCarrinhoDaSessaoAtual() {
        var carrinhoJson = HttpContext.Session.GetString(CarrinhoSessionKey);
        return DeserializarCarrinho(carrinhoJson);
    }

    private List<CarrinhoSessionItem> DeserializarCarrinho(string? carrinhoJson) {
        if (string.IsNullOrWhiteSpace(carrinhoJson)) {
            return new List<CarrinhoSessionItem>();
        }

        return JsonSerializer.Deserialize<List<CarrinhoSessionItem>>(carrinhoJson) ?? new List<CarrinhoSessionItem>();
    }

    private List<CarrinhoSessionItem> MesclarCarrinhos(List<CarrinhoSessionItem> carrinhoPersistido, List<CarrinhoSessionItem> carrinhoSessao) {
        var itens = carrinhoPersistido
            .Concat(carrinhoSessao)
            .GroupBy(i => i.LivroId)
            .Select(g => new CarrinhoSessionItem {
                LivroId = g.Key,
                Quantidade = g.Sum(x => x.Quantidade)
            })
            .Where(i => i.Quantidade > 0)
            .ToList();

        return itens;
    }

    private void TransferirReservasSessaoParaCliente(int clienteId, string? sessionKey) {
        if (string.IsNullOrWhiteSpace(sessionKey)) {
            return;
        }

        var reservasSessao = _context.ReservasCarrinho
            .Where(r => r.SessionKey == sessionKey && r.ClienteId == null)
            .ToList();

        foreach (var reserva in reservasSessao) {
            reserva.ClienteId = clienteId;
            reserva.SessionKey = null;
        }
    }

    private sealed class CarrinhoSessionItem {
        public int LivroId { get; set; }
        public int Quantidade { get; set; }
    }
}
