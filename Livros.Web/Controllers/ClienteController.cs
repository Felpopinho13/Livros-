using Livros.Domain;
using Livros.Infrastructure.Services;
using Livros.Infrastructure.Data;
using Livros.Web.Helpers;
using Livros.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

        var cliente = _context.Clientes
            .Include(c => c.Enderecos)
            .Include(c => c.Cartoes)
            .FirstOrDefault(c => c.Email == usuario && c.IsAtivo);

        if (cliente == null) {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        var pedidos = _context.Pedidos
            .Where(p => p.ClienteId == cliente.Id)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Livro)
            .OrderByDescending(p => p.Data)
            .ToList();

        var ultimoPedido = pedidos.FirstOrDefault();
        var ranking = ClienteRankingHelper.ObterRanking(ClienteRankingHelper.CalcularValorElegivel(pedidos));

        var trocasAbertas = _context.Trocas.Count(t =>
            t.ClienteId == cliente.Id &&
            t.Status != "TROCADO" &&
            t.Status != "Recebida" &&
            t.Status != "Aprovado" &&
            t.Status != "TROCA RECUSADA" &&
            t.Status != "Recusado");

        var cuponsDisponiveis = _context.CuponsDesconto
            .Where(c => c.ClienteId == cliente.Id && c.IsAtivo && c.DataUtilizacao == null)
            .OrderByDescending(c => c.DataCriacao)
            .ToList();

        var carrinhoJson = HttpContext.Session.GetString("Carrinho");
        var itensNoCarrinho = 0;

        if (!string.IsNullOrWhiteSpace(carrinhoJson)) {
            var itensCarrinho = JsonSerializer.Deserialize<List<CarrinhoResumoSessionItem>>(carrinhoJson)
                ?? new List<CarrinhoResumoSessionItem>();

            itensNoCarrinho = itensCarrinho.Sum(i => i.Quantidade);
        }

        var nomeExibicao = string.IsNullOrWhiteSpace(cliente.Nome)
            ? cliente.Email
            : cliente.Nome;

        var primeiroNome = nomeExibicao.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nomeExibicao;

        var vm = new AreaClienteViewModel {
            NomeExibicao = nomeExibicao,
            PrimeiroNome = primeiroNome,
            Email = cliente.Email,
            TotalPedidos = pedidos.Count,
            ValorTotalCompras = pedidos.Sum(p => p.Total),
            QuantidadeEnderecos = cliente.Enderecos?.Count ?? 0,
            QuantidadeCartoes = cliente.Cartoes?.Count ?? 0,
            QuantidadeCuponsDisponiveis = cuponsDisponiveis.Count,
            QuantidadeTrocasAbertas = trocasAbertas,
            ItensNoCarrinho = itensNoCarrinho,
            RankingNome = ranking.Nome,
            RankingCssClass = ranking.CssClass,
            ValorElegivelRanking = ranking.ValorElegivel,
            ProximoMarcoRanking = ranking.ProximoMarco,
            ProximoRankingNome = ranking.ProximoNome,
            UltimoPedido = ultimoPedido == null ? null : new AreaClientePedidoResumoViewModel {
                Id = ultimoPedido.Id,
                Data = ultimoPedido.Data,
                Total = ultimoPedido.Total,
                Status = ultimoPedido.Status,
                QuantidadeItens = ultimoPedido.Itens?.Sum(i => i.Quantidade) ?? 0,
                LivroPrincipal = ultimoPedido.Itens?.FirstOrDefault()?.Livro?.Titulo ?? "Pedido sem itens"
            },
            UltimoCupomDisponivel = cuponsDisponiveis
                .Select(c => new AreaClienteCupomResumoViewModel {
                    Codigo = c.Codigo,
                    Valor = c.Valor,
                    Tipo = c.Tipo
                })
                .FirstOrDefault()
        };

        return View(vm);
    }

    public IActionResult Cupons() {
        var usuario = HttpContext.Session.GetString("Usuario");

        if (usuario == null) {
            return RedirectToAction("Login", "Auth");
        }

        var cliente = _context.Clientes
            .FirstOrDefault(c => c.Email == usuario && c.IsAtivo);

        if (cliente == null) {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        var cupons = _context.CuponsDesconto
            .Where(c => c.ClienteId == cliente.Id)
            .OrderByDescending(c => c.DataCriacao)
            .ToList();

        var vm = new MeusCuponsViewModel {
            NomeCliente = string.IsNullOrWhiteSpace(cliente.Nome) ? cliente.Email : cliente.Nome,
            TotalCupons = cupons.Count,
            CuponsDisponiveis = cupons.Count(c => c.IsAtivo && c.DataUtilizacao == null),
            ValorDisponivel = cupons
                .Where(c => c.IsAtivo && c.DataUtilizacao == null)
                .Sum(c => c.Valor),
            Cupons = cupons.Select(c => new MeuCupomItemViewModel {
                Codigo = c.Codigo,
                Tipo = c.Tipo,
                Valor = c.Valor,
                DataCriacao = c.DataCriacao,
                DataUtilizacao = c.DataUtilizacao,
                PedidoId = c.PedidoId,
                Status = c.DataUtilizacao != null
                    ? "Utilizado"
                    : c.IsAtivo
                        ? "Disponível"
                        : "Inativo",
                Descricao = c.Tipo == "TROCA"
                    ? "Cupom de troca liberado a partir de uma solicitacao aprovada. Pode ser usado uma unica vez no checkout."
                    : "Cupom promocional para abater o valor dos produtos. Pode ser usado uma unica vez no checkout."
            }).ToList()
        };

        return View(vm);
    }

    public IActionResult Editar() {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var cliente = _service.BuscarPorEmail(email);

        return View(cliente);
    }

    [HttpGet]
    public IActionResult AlterarSenha() {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var cliente = _service.BuscarPorEmail(email);

        if (cliente == null)
            return RedirectToAction("Login", "Auth");

        return View(cliente);
    }

    [HttpPost]
    public IActionResult Editar(Cliente cliente) {

        var emailExistente = _context.Clientes
            .FirstOrDefault(c => c.Email == cliente.Email && c.Id != cliente.Id);

        if (emailExistente != null) {
            TempData["Erro"] = "Este email já está em uso.";
            return View(cliente);
        }

        _service.Atualizar(cliente);

        HttpContext.Session.SetString("Usuario", cliente.Email);

        TempData["Sucesso"] = "Dados atualizados com sucesso!";

        return RedirectToAction("Editar");
    }

    [HttpPost]
    public IActionResult ExcluirConta() {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var cliente = _context.Clientes
            .FirstOrDefault(c => c.Email == email);

        if (cliente == null)
            return NotFound();

        cliente.IsAtivo = false;

        _context.SaveChanges();

        HttpContext.Session.Clear();

        return RedirectToAction("Index", "Home");
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
            TipoLogradouro = endereco.TipoLogradouro,
            Logradouro = endereco.Logradouro,
            Numero = endereco.Numero,
            Complemento = endereco.Complemento,
            TipoResidencia = endereco.TipoResidencia,
            Pais = endereco.Pais,
            IsEntrega = endereco.IsEntrega,
            IsCobranca = endereco.IsCobranca,
            Bairro = endereco.Bairro.Nome,
            Cidade = endereco.Cidade.Nome,
            Estado = endereco.Cidade.Estado.Sigla
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult EditarEndereco(EnderecoViewModel vm) {
        var endereco = _context.Enderecos
            .Include(e => e.Cliente)
            .Include(e => e.Cidade)
            .Include(e => e.Bairro)
            .FirstOrDefault(e => e.Id == vm.Id);

        if (endereco == null)
            return NotFound();

        if (!ValidarFinalidadeEndereco(vm.IsEntrega, vm.IsCobranca)) {
            ModelState.AddModelError(string.Empty, "Selecione pelo menos uma finalidade para o endereco: entrega, cobranca ou ambas.");
            return View(vm);
        }

        var clienteId = endereco.ClienteId;
        if (!ClienteMantemEnderecoObrigatorio(clienteId, endereco.Id, vm.IsEntrega, vm.IsCobranca, out var erroFinalidade)) {
            ModelState.AddModelError(string.Empty, erroFinalidade!);
            return View(vm);
        }

        endereco.NomeEndereco = vm.NomeEndereco;
        endereco.CEP = vm.CEP;
        endereco.TipoLogradouro = vm.TipoLogradouro;
        endereco.Logradouro = vm.Logradouro;
        endereco.Numero = vm.Numero;
        endereco.Complemento = vm.Complemento;
        endereco.TipoResidencia = vm.TipoResidencia;
        endereco.Pais = vm.Pais;
        endereco.IsEntrega = vm.IsEntrega;
        endereco.IsCobranca = vm.IsCobranca;

        var estado = _context.Estados.FirstOrDefault(e => e.Sigla == vm.Estado);
        if (estado == null) {
            estado = new Estado { Nome = vm.Estado.Trim().ToUpperInvariant(), Sigla = vm.Estado.Trim().ToUpperInvariant() };
            _context.Estados.Add(estado);
            _context.SaveChanges();
        }

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

        NormalizarEndereco(endereco);
        AjustarEnderecoPadraoEntrega(clienteId, endereco.Id);
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
                .ThenInclude(e => e.Cidade)
                    .ThenInclude(c => c.Estado)
            .Include(c => c.Enderecos)
                .ThenInclude(e => e.Bairro)
            .FirstOrDefault(c => c.Email == email);

        if (cliente == null)
            return RedirectToAction("Login", "Auth"); 

        var enderecos = cliente.Enderecos
            .OrderByDescending(e => e.IsPadrao)
            .ThenByDescending(e => e.IsEntrega)
            .ThenBy(e => e.NomeEndereco)
            .ToList();

        return View(enderecos);
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

        var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);
        if (cliente == null)
            return RedirectToAction("Login", "Auth");

        if (!ValidarFinalidadeEndereco(isEntrega, isCobranca)) {
            TempData["Erro"] = "Selecione pelo menos uma finalidade para o endereco: entrega, cobranca ou ambas.";
            return RedirectToAction("Enderecos");
        }

        if (!ClienteMantemEnderecoObrigatorio(id, null, isEntrega, isCobranca, out var erroCadastro)) {
            TempData["Erro"] = erroCadastro;
            return RedirectToAction("Enderecos");
        }

        var estadoSigla = estado.Trim().ToUpperInvariant();
        var estadoEntity = _context.Estados.FirstOrDefault(e => e.Sigla == estadoSigla);
        if (estadoEntity == null) {
            estadoEntity = new Estado { Nome = estadoSigla, Sigla = estadoSigla };
            _context.Estados.Add(estadoEntity);
            _context.SaveChanges();
        }

        var cidadeEntity = _context.Cidades
            .FirstOrDefault(c => c.Nome == cidade && c.EstadoId == estadoEntity.Id);
        if (cidadeEntity == null) {
            cidadeEntity = new Cidade { Nome = cidade, EstadoId = estadoEntity.Id };
            _context.Cidades.Add(cidadeEntity);
            _context.SaveChanges();
        }

        var bairroEntity = _context.Bairros
            .FirstOrDefault(b => b.Nome == bairro && b.CidadeId == cidadeEntity.Id);
        if (bairroEntity == null) {
            bairroEntity = new Bairro { Nome = bairro, CidadeId = cidadeEntity.Id };
            _context.Bairros.Add(bairroEntity);
            _context.SaveChanges();
        }

        var endereco = new Endereco {
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
            IsPadrao = !_context.Enderecos.Any(e => e.ClienteId == cliente.Id && e.IsEntrega),
            CidadeId = cidadeEntity.Id,
            BairroId = bairroEntity.Id,
            ClienteId = cliente.Id
        };

        NormalizarEndereco(endereco);
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

        var endereco = cliente.Enderecos.FirstOrDefault(e => e.Id == id);
        if (endereco == null || !endereco.IsEntrega) {
            TempData["Erro"] = "Somente enderecos de entrega podem ser definidos como padrao.";
            return RedirectToAction("Enderecos");
        }

        foreach (var e in cliente.Enderecos) {
            e.IsPadrao = false;
        }

        endereco.IsPadrao = true;

        _context.SaveChanges();

        return RedirectToAction("Enderecos");
    }

    [HttpPost]
    public IActionResult ExcluirEndereco(int id) {
        var email = HttpContext.Session.GetString("Usuario");

        if (email == null)
            return RedirectToAction("Login", "Auth");

        var endereco = _context.Enderecos
            .Include(e => e.Cliente)
            .FirstOrDefault(e => e.Id == id && e.Cliente.Email == email);

        if (endereco == null) {
            TempData["Erro"] = "Endereço não encontrado.";
            return RedirectToAction("Enderecos");
        }

        var enderecoEmUso = _context.Pedidos.Any(p => p.EnderecoId == endereco.Id);
        if (enderecoEmUso) {
            TempData["Erro"] = "Este endereço já foi usado em um pedido e não pode ser excluído.";
            return RedirectToAction("Enderecos");
        }

        if (!ClientePodeExcluirEndereco(endereco.ClienteId, endereco.Id, out var erroExclusao)) {
            TempData["Erro"] = erroExclusao;
            return RedirectToAction("Enderecos");
        }

        _context.Enderecos.Remove(endereco);
        _context.SaveChanges();

        AjustarEnderecoPadraoEntrega(endereco.ClienteId);
        _context.SaveChanges();

        TempData["Sucesso"] = "Endereço excluído com sucesso!";

        return RedirectToAction("Enderecos");
    }

    public IActionResult Cartoes() {
        var email = HttpContext.Session.GetString("Usuario");

        var cliente = _context.Clientes
            .Include(c => c.Cartoes)
                .ThenInclude(c => c.BandeiraCartao)
            .FirstOrDefault(c => c.Email == email);

        if (cliente == null)
            return RedirectToAction("Login", "Auth");

        var vm = new CartoesViewModel {
            Cartoes = cliente.Cartoes?
                .OrderByDescending(c => c.IsPadrao)
                .ThenBy(c => c.NomeImpresso)
                .ToList() ?? new List<Cartao>(),
            Bandeiras = _context.BandeirasCartao
                .Where(b => b.IsAtiva)
                .OrderBy(b => b.Nome)
                .ToList()
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

        var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);

        if (cliente == null)
            return RedirectToAction("Login", "Auth");

        var bandeira = _context.BandeirasCartao
            .FirstOrDefault(b => b.Id == bandeiraCartaoId && b.IsAtiva);

        if (bandeira == null) {
            TempData["Erro"] = "Selecione uma bandeira de cartao valida.";
            return RedirectToAction("Cartoes");
        }

        if (string.IsNullOrWhiteSpace(nome) ||
            string.IsNullOrWhiteSpace(numero) ||
            string.IsNullOrWhiteSpace(validade) ||
            string.IsNullOrWhiteSpace(cvv)) {
            TempData["Erro"] = "Preencha todos os dados obrigatorios do cartao.";
            return RedirectToAction("Cartoes");
        }

        var numeroNormalizado = NormalizarDigitos(numero);
        if (numeroNormalizado.Length != 16) {
            TempData["Erro"] = "O numero do cartao deve ter exatamente 16 digitos.";
            return RedirectToAction("Cartoes");
        }

        var cvvNormalizado = NormalizarDigitos(cvv);
        if (cvvNormalizado.Length != 3) {
            TempData["Erro"] = "O CVV deve ter exatamente 3 digitos.";
            return RedirectToAction("Cartoes");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(validade.Trim(), "^(0[1-9]|1[0-2])\\/\\d{2}$")) {
            TempData["Erro"] = "A validade deve estar no formato MM/AA.";
            return RedirectToAction("Cartoes");
        }

        var cartao = new Cartao {
            NomeImpresso = nome.Trim(),
            Numero = numeroNormalizado,
            Validade = validade.Trim(),
            CVV = cvvNormalizado,
            BandeiraCartaoId = bandeira.Id,
            ClienteId = cliente.Id
        };

        _context.Cartoes.Add(cartao);
        _context.SaveChanges();

        TempData["Sucesso"] = "Cartao cadastrado com sucesso!";

        return RedirectToAction("Cartoes");
    }

    public IActionResult TornarCartaoPadrao(int id) {
        var email = HttpContext.Session.GetString("Usuario");

        var cliente = _context.Clientes
            .Include(c => c.Cartoes)
            .FirstOrDefault(c => c.Email == email);

        foreach (var c in cliente.Cartoes)
            c.IsPadrao = false;

        var cartao = cliente.Cartoes.FirstOrDefault(c => c.Id == id);

        if (cartao != null)
            cartao.IsPadrao = true;

        _context.SaveChanges();

        return RedirectToAction("Cartoes");
    }

    [HttpPost]
    public IActionResult ExcluirCartao(int id) {
        var cartao = _context.Cartoes.FirstOrDefault(c => c.Id == id);

        if (cartao != null) {
            _context.Cartoes.Remove(cartao);
            _context.SaveChanges();
        }

        return RedirectToAction("Cartoes");
    }

    [HttpPost]
    public IActionResult AlterarSenha(string senhaAtual, string novaSenha, string confirmarSenha) {
        if (novaSenha != confirmarSenha) {
            TempData["Erro"] = "As senhas não coincidem.";
            return RedirectToAction("AlterarSenha");
        }

        if (!PasswordPolicyHelper.IsStrongPassword(novaSenha)) {
            TempData["Erro"] = PasswordPolicyHelper.MensagemRequisito;
            return RedirectToAction("AlterarSenha");
        }

        var idStr = HttpContext.Session.GetString("ClienteId");

        if (idStr == null)
            return RedirectToAction("Login", "Auth");

        var id = int.Parse(idStr);

        var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);

        if (cliente == null)
            return RedirectToAction("Login", "Auth");

        if (!BCrypt.Net.BCrypt.Verify(senhaAtual, cliente.Senha)) {
            TempData["Erro"] = "A senha atual informada está incorreta.";
            return RedirectToAction("AlterarSenha");
        }

        cliente.Senha = BCrypt.Net.BCrypt.HashPassword(novaSenha);

        _context.SaveChanges();

        TempData["Sucesso"] = "Senha alterada com sucesso!";

        return RedirectToAction("AlterarSenha");
    }

    private sealed class CarrinhoResumoSessionItem {
        public int LivroId { get; set; }
        public int Quantidade { get; set; }
    }

    private bool ValidarFinalidadeEndereco(bool isEntrega, bool isCobranca) {
        return isEntrega || isCobranca;
    }

    private bool ClienteMantemEnderecoObrigatorio(int clienteId, int? enderecoIgnoradoId, bool isEntrega, bool isCobranca, out string? erro) {
        erro = null;

        var outrosEnderecos = _context.Enderecos
            .Where(e => e.ClienteId == clienteId && (!enderecoIgnoradoId.HasValue || e.Id != enderecoIgnoradoId.Value))
            .ToList();

        var teriaEntrega = isEntrega || outrosEnderecos.Any(e => e.IsEntrega);
        var teriaCobranca = isCobranca || outrosEnderecos.Any(e => e.IsCobranca);

        if (!teriaEntrega) {
            erro = "O cliente precisa manter pelo menos um endereco de entrega.";
            return false;
        }

        if (!teriaCobranca) {
            erro = "O cliente precisa manter pelo menos um endereco de cobranca.";
            return false;
        }

        return true;
    }

    private bool ClientePodeExcluirEndereco(int clienteId, int enderecoId, out string? erro) {
        erro = null;

        var restantes = _context.Enderecos
            .Where(e => e.ClienteId == clienteId && e.Id != enderecoId)
            .ToList();

        if (!restantes.Any(e => e.IsEntrega)) {
            erro = "O cliente precisa manter pelo menos um endereco de entrega.";
            return false;
        }

        if (!restantes.Any(e => e.IsCobranca)) {
            erro = "O cliente precisa manter pelo menos um endereco de cobranca.";
            return false;
        }

        return true;
    }

    private void AjustarEnderecoPadraoEntrega(int clienteId, int? enderecoPreferencialId = null) {
        var enderecos = _context.Enderecos
            .Where(e => e.ClienteId == clienteId)
            .OrderByDescending(e => e.Id == enderecoPreferencialId)
            .ThenBy(e => e.Id)
            .ToList();

        var enderecosEntrega = enderecos.Where(e => e.IsEntrega).ToList();
        if (!enderecosEntrega.Any()) {
            return;
        }

        var padraoAtual = enderecosEntrega.FirstOrDefault(e => e.IsPadrao);
        if (padraoAtual != null) {
            foreach (var endereco in enderecos.Where(e => !e.IsEntrega)) {
                endereco.IsPadrao = false;
            }
            return;
        }

        foreach (var endereco in enderecos) {
            endereco.IsPadrao = false;
        }

        enderecosEntrega.First().IsPadrao = true;
    }

    private void NormalizarEndereco(Endereco endereco) {
        endereco.NomeEndereco = endereco.NomeEndereco?.Trim() ?? string.Empty;
        endereco.CEP = endereco.CEP?.Trim() ?? string.Empty;
        endereco.TipoLogradouro = endereco.TipoLogradouro?.Trim() ?? string.Empty;
        endereco.Logradouro = endereco.Logradouro?.Trim() ?? string.Empty;
        endereco.Numero = endereco.Numero?.Trim() ?? string.Empty;
        endereco.Complemento = string.IsNullOrWhiteSpace(endereco.Complemento) ? null : endereco.Complemento.Trim();
        endereco.TipoResidencia = endereco.TipoResidencia?.Trim() ?? string.Empty;
        endereco.Pais = endereco.Pais?.Trim() ?? "Brasil";
    }

    private string NormalizarDigitos(string? valor) {
        if (string.IsNullOrWhiteSpace(valor)) {
            return string.Empty;
        }

        return new string(valor.Where(char.IsDigit).ToArray());
    }
}

