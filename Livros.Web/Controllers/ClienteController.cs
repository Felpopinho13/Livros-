using Livros.Application.CustomerAccounts;
using Livros.Application.CustomerAddresses;
using Livros.Application.CustomerCards;
using Livros.Domain;
using Livros.Infrastructure.Services;
using Livros.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

public class ClienteController : Controller {
    private readonly ClienteService _service;
    private readonly CustomerAddressService _customerAddressService;
    private readonly CustomerAccountService _customerAccountService;
    private readonly CustomerCardService _customerCardService;

    public ClienteController(
        ClienteService service,
        CustomerAddressService customerAddressService,
        CustomerAccountService customerAccountService,
        CustomerCardService customerCardService) {
        _service = service;
        _customerAddressService = customerAddressService;
        _customerAccountService = customerAccountService;
        _customerCardService = customerCardService;
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

        var result = _customerAccountService.GetDashboard(new CustomerDashboardQuery {
            Email = usuario,
            CartItemCount = GetCartItemCount()
        });

        if (!result.CustomerFound) {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        var vm = new AreaClienteViewModel {
            NomeExibicao = result.NomeExibicao,
            PrimeiroNome = result.PrimeiroNome,
            Email = result.Email,
            TotalPedidos = result.TotalPedidos,
            ValorTotalCompras = result.ValorTotalCompras,
            QuantidadeEnderecos = result.QuantidadeEnderecos,
            QuantidadeCartoes = result.QuantidadeCartoes,
            QuantidadeCuponsDisponiveis = result.QuantidadeCuponsDisponiveis,
            QuantidadeTrocasAbertas = result.QuantidadeTrocasAbertas,
            ItensNoCarrinho = result.ItensNoCarrinho,
            RankingNome = result.RankingNome,
            RankingCssClass = result.RankingCssClass,
            ValorElegivelRanking = result.ValorElegivelRanking,
            ProximoMarcoRanking = result.ProximoMarcoRanking,
            ProximoRankingNome = result.ProximoRankingNome,
            UltimoPedido = result.UltimoPedido == null ? null : new AreaClientePedidoResumoViewModel {
                Id = result.UltimoPedido.Id,
                Data = result.UltimoPedido.Data,
                Total = result.UltimoPedido.Total,
                Status = result.UltimoPedido.Status,
                QuantidadeItens = result.UltimoPedido.QuantidadeItens,
                LivroPrincipal = result.UltimoPedido.LivroPrincipal
            },
            UltimoCupomDisponivel = result.UltimoCupomDisponivel == null ? null : new AreaClienteCupomResumoViewModel {
                Codigo = result.UltimoCupomDisponivel.Codigo,
                Valor = result.UltimoCupomDisponivel.Valor,
                Tipo = result.UltimoCupomDisponivel.Tipo
            }
        };

        return View(vm);
    }

    public IActionResult Cupons() {
        var usuario = HttpContext.Session.GetString("Usuario");

        if (usuario == null) {
            return RedirectToAction("Login", "Auth");
        }

        var result = _customerAccountService.GetCoupons(new CustomerCouponsQuery {
            Email = usuario
        });

        if (!result.CustomerFound) {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        var vm = new MeusCuponsViewModel {
            NomeCliente = result.NomeCliente,
            TotalCupons = result.TotalCupons,
            CuponsDisponiveis = result.CuponsDisponiveis,
            ValorDisponivel = result.ValorDisponivel,
            Cupons = result.Cupons.Select(c => new MeuCupomItemViewModel {
                Codigo = c.Codigo,
                Tipo = c.Tipo,
                Valor = c.Valor,
                DataCriacao = c.DataCriacao,
                DataUtilizacao = c.DataUtilizacao,
                PedidoId = c.PedidoId,
                Status = c.Status,
                Descricao = c.Descricao
            }).ToList()
        };

        return View(vm);
    }

    public IActionResult Editar() {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null) {
            return RedirectToAction("Login", "Auth");
        }

        var result = _customerAccountService.GetProfile(new CustomerProfileQuery {
            Email = email
        });

        if (!result.CustomerFound || result.Customer == null) {
            return RedirectToAction("Login", "Auth");
        }

        return View(result.Customer);
    }

    [HttpGet]
    public IActionResult AlterarSenha() {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null) {
            return RedirectToAction("Login", "Auth");
        }

        var result = _customerAccountService.GetProfile(new CustomerProfileQuery {
            Email = email
        });

        if (!result.CustomerFound || result.Customer == null) {
            return RedirectToAction("Login", "Auth");
        }

        return View(result.Customer);
    }

    [HttpPost]
    public IActionResult Editar(Cliente cliente) {
        var result = _customerAccountService.UpdateProfile(new CustomerProfileUpdateCommand {
            CustomerId = cliente.Id,
            Nome = cliente.Nome,
            Email = cliente.Email,
            Telefone = cliente.Telefone,
            CPF = cliente.CPF
        });

        if (!result.CustomerFound) {
            return RedirectToAction("Login", "Auth");
        }

        if (!result.Success) {
            TempData["Erro"] = result.ErrorMessage ?? "Nao foi possivel atualizar os dados.";
            return View(cliente);
        }

        HttpContext.Session.SetString("Usuario", result.UpdatedEmail ?? cliente.Email);

        TempData["Sucesso"] = "Dados atualizados com sucesso!";

        return RedirectToAction("Editar");
    }

    [HttpPost]
    public IActionResult ExcluirConta() {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null) {
            return RedirectToAction("Login", "Auth");
        }

        var result = _customerAccountService.DeactivateAccount(email);
        if (!result.CustomerFound) {
            return NotFound();
        }

        HttpContext.Session.Clear();

        return RedirectToAction("Index", "Home");
    }

    public IActionResult EditarEndereco(int id) {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var result = _customerAddressService.GetForEdit(new CustomerAddressEditQuery {
            Email = email,
            AddressId = id
        });

        if (!result.Found || result.Address == null)
            return NotFound();

        var vm = new EnderecoViewModel {
            Id = result.Address.Id,
            NomeEndereco = result.Address.NomeEndereco,
            CEP = result.Address.CEP,
            TipoLogradouro = result.Address.TipoLogradouro,
            Logradouro = result.Address.Logradouro,
            Numero = result.Address.Numero,
            Complemento = result.Address.Complemento,
            TipoResidencia = result.Address.TipoResidencia,
            Pais = result.Address.Pais,
            IsEntrega = result.Address.IsEntrega,
            IsCobranca = result.Address.IsCobranca,
            Bairro = result.Address.Bairro,
            Cidade = result.Address.Cidade,
            Estado = result.Address.Estado
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult EditarEndereco(EnderecoViewModel vm) {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var result = _customerAddressService.Update(new CustomerAddressUpdateCommand {
            Email = email,
            AddressId = vm.Id,
            NomeEndereco = vm.NomeEndereco,
            CEP = vm.CEP,
            TipoLogradouro = vm.TipoLogradouro,
            Logradouro = vm.Logradouro,
            Numero = vm.Numero,
            Complemento = vm.Complemento,
            TipoResidencia = vm.TipoResidencia,
            Pais = vm.Pais,
            IsEntrega = vm.IsEntrega,
            IsCobranca = vm.IsCobranca,
            Bairro = vm.Bairro,
            Cidade = vm.Cidade,
            Estado = vm.Estado
        });

        if (!result.AddressFound)
            return NotFound();

        if (!result.Success) {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Nao foi possivel atualizar o endereco.");
            return View(vm);
        }

        TempData["Sucesso"] = "Endereço atualizado com sucesso!";

        return RedirectToAction("EditarEndereco", new { id = vm.Id });
    }

    public IActionResult Enderecos() {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var result = _customerAddressService.List(new CustomerAddressListQuery {
            Email = email
        });

        if (!result.CustomerFound)
            return RedirectToAction("Login", "Auth");

        return View(result.Addresses);
    }

    [HttpPost]
    public IActionResult CadastrarEndereco(
    string nomeEndereco,
    string cep,
    string tipoLogradouro,
    string logradouro,
    string numero,
    string complemento,
    string tipoResidencia,
    string pais,
    bool isEntrega,
    bool isCobranca,
    string bairro,
    string cidade,
    string estado) {
        var idStr = HttpContext.Session.GetString("ClienteId");

        if (idStr == null)
            return RedirectToAction("Login", "Auth");

        var id = int.Parse(idStr);

        var result = _customerAddressService.Create(new CustomerAddressCreateCommand {
            ClienteId = id,
            NomeEndereco = nomeEndereco,
            CEP = cep,
            TipoLogradouro = tipoLogradouro,
            Logradouro = logradouro,
            Numero = numero,
            Complemento = complemento,
            TipoResidencia = tipoResidencia,
            Pais = pais,
            IsEntrega = isEntrega,
            IsCobranca = isCobranca,
            Bairro = bairro,
            Cidade = cidade,
            Estado = estado
        });

        if (!result.CustomerFound)
            return RedirectToAction("Login", "Auth");

        if (!result.Success) {
            TempData["Erro"] = result.ErrorMessage;
            return RedirectToAction("Enderecos");
        }

        TempData["Sucesso"] = "Endereço cadastrado com sucesso!";

        return RedirectToAction("Enderecos");
    }

    public IActionResult TornarPadrao(int id) {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var result = _customerAddressService.SetDefault(new CustomerAddressSetDefaultCommand {
            Email = email,
            AddressId = id
        });

        if (!result.CustomerFound)
            return RedirectToAction("Login", "Auth");

        if (!result.Success) {
            TempData["Erro"] = result.ErrorMessage;
            return RedirectToAction("Enderecos");
        }

        return RedirectToAction("Enderecos");
    }

    [HttpPost]
    public IActionResult ExcluirEndereco(int id) {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var result = _customerAddressService.Delete(new CustomerAddressDeleteCommand {
            Email = email,
            AddressId = id
        });

        if (!result.AddressFound) {
            TempData["Erro"] = "Endereço não encontrado.";
            return RedirectToAction("Enderecos");
        }

        if (!result.Success) {
            TempData["Erro"] = result.ErrorMessage;
            return RedirectToAction("Enderecos");
        }

        TempData["Sucesso"] = "Endereço excluído com sucesso!";

        return RedirectToAction("Enderecos");
    }

    public IActionResult Cartoes() {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var result = _customerCardService.List(new CustomerCardsQuery {
            Email = email
        });

        if (!result.CustomerFound)
            return RedirectToAction("Login", "Auth");

        var vm = new CartoesViewModel {
            Cartoes = result.Cards,
            Bandeiras = result.Brands
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult CadastrarCartao(
    string nome,
    string numero,
    int bandeiraCartaoId,
    string validade,
    string cvv) {
        var idStr = HttpContext.Session.GetString("ClienteId");

        if (idStr == null)
            return RedirectToAction("Login", "Auth");

        var id = int.Parse(idStr);

        var result = _customerCardService.Create(new CustomerCardCreateCommand {
            ClienteId = id,
            Nome = nome,
            Numero = numero,
            BandeiraCartaoId = bandeiraCartaoId,
            Validade = validade,
            Cvv = cvv
        });

        if (!result.CustomerFound)
            return RedirectToAction("Login", "Auth");

        if (!result.Success) {
            TempData["Erro"] = result.ErrorMessage;
            return RedirectToAction("Cartoes");
        }

        TempData["Sucesso"] = "Cartao cadastrado com sucesso!";

        return RedirectToAction("Cartoes");
    }

    public IActionResult TornarCartaoPadrao(int id) {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var result = _customerCardService.SetDefault(new CustomerCardSetDefaultCommand {
            Email = email,
            CardId = id
        });

        if (!result.CustomerFound) {
            TempData["Erro"] = result.ErrorMessage ?? "Cliente ou cartoes nao encontrados.";
            return RedirectToAction("Cartoes");
        }

        if (!result.Success) {
            TempData["Erro"] = result.ErrorMessage ?? (result.CardFound ? "Nao foi possivel definir o cartao padrao." : "Cartao nao encontrado.");
            return RedirectToAction("Cartoes");
        }

        return RedirectToAction("Cartoes");
    }

    [HttpPost]
    public IActionResult ExcluirCartao(int id) {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var result = _customerCardService.Delete(new CustomerCardDeleteCommand {
            Email = email,
            CardId = id
        });

        if (!result.CardFound) {
            TempData["Erro"] = "Cartao nao encontrado.";
            return RedirectToAction("Cartoes");
        }

        if (!result.Success) {
            TempData["Erro"] = result.ErrorMessage;
            return RedirectToAction("Cartoes");
        }

        return RedirectToAction("Cartoes");
    }

    [HttpPost]
    public IActionResult AlterarSenha(string senhaAtual, string novaSenha, string confirmarSenha) {
        var idStr = HttpContext.Session.GetString("ClienteId");

        if (idStr == null)
            return RedirectToAction("Login", "Auth");

        var id = int.Parse(idStr);

        var result = _customerAccountService.ChangePassword(new CustomerPasswordChangeCommand {
            CustomerId = id,
            CurrentPassword = senhaAtual,
            NewPassword = novaSenha,
            ConfirmPassword = confirmarSenha
        });

        if (!result.CustomerFound)
            return RedirectToAction("Login", "Auth");

        if (!result.Success) {
            TempData["Erro"] = result.ErrorMessage ?? "Nao foi possivel alterar a senha.";
            return RedirectToAction("AlterarSenha");
        }

        TempData["Sucesso"] = "Senha alterada com sucesso!";

        return RedirectToAction("AlterarSenha");
    }

    private int GetCartItemCount() {
        var carrinhoJson = HttpContext.Session.GetString("Carrinho");
        if (string.IsNullOrWhiteSpace(carrinhoJson)) {
            return 0;
        }

        var itensCarrinho = JsonSerializer.Deserialize<List<CarrinhoResumoSessionItem>>(carrinhoJson)
            ?? new List<CarrinhoResumoSessionItem>();

        return itensCarrinho.Sum(i => i.Quantidade);
    }

    private sealed class CarrinhoResumoSessionItem {
        public int LivroId { get; set; }
        public int Quantidade { get; set; }
    }
}
