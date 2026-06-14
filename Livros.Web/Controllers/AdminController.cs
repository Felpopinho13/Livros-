using Livros.Domain;
using Livros.Application.SalesAnalysis;
using Livros.Application.AdminOrders;
using Livros.Application.AdminCustomers;
using Livros.Application.AdminExchanges;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Services;
using Livros.Web.Helpers;
using Livros.Web.Models.ViewModels;
using Livros.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

public class AdminController : Controller {
        private readonly AppDbContext _context;
    private readonly AdminCustomersService _adminCustomersService;
    private readonly AdminExchangesService _adminExchangesService;
    private readonly AdminOrdersService _adminOrdersService;
    private readonly SalesAnalysisService _salesAnalysisService;
    private readonly LivroService _livroService;
    private readonly EstoqueService _estoqueService;
    private readonly AdminSalesHistorySeedService _adminSalesHistorySeedService;

        public AdminController(AppDbContext context, AdminCustomersService adminCustomersService, AdminExchangesService adminExchangesService, AdminOrdersService adminOrdersService, SalesAnalysisService salesAnalysisService, LivroService livroService, EstoqueService estoqueService, AdminSalesHistorySeedService adminSalesHistorySeedService) {
        _context = context;
        _adminCustomersService = adminCustomersService;
        _adminExchangesService = adminExchangesService;
        _adminOrdersService = adminOrdersService;
        _salesAnalysisService = salesAnalysisService;
        _livroService = livroService;
        _estoqueService = estoqueService;
        _adminSalesHistorySeedService = adminSalesHistorySeedService;
    }

    public IActionResult Dashboard() {
        return View();
    }

        public async Task<IActionResult> Clientes(
        string? busca,
        string? nome,
        string? email,
        string? cpf,
        string? telefone,
        string? genero,
        string? dataNascimento,
        string? status,
        string? admin,
        int pagina = 1,
        CancellationToken cancellationToken = default) {
        var result = await _adminCustomersService.BuildAsync(
            new AdminCustomersQuery {
                Busca = busca,
                Nome = nome,
                Email = email,
                Cpf = cpf,
                Telefone = telefone,
                Genero = genero,
                DataNascimento = dataNascimento,
                Status = status,
                Admin = admin,
                Pagina = pagina
            },
            cancellationToken);

        return View(AdminClientesViewModelMapper.Map(result));
    }

    [HttpGet]
        public async Task<IActionResult> ClienteTransacoes(int id, CancellationToken cancellationToken = default) {
        var result = await _adminCustomersService.BuildTransactionsAsync(id, cancellationToken);

        if (result == null) {
            TempData["Erro"] = "Cliente nao encontrado.";
            return RedirectToAction("Clientes");
        }

        return View(AdminClienteTransacoesViewModelMapper.Map(result));
    }

    [HttpPost]
    public IActionResult CriarClienteAdmin(Cliente cliente) {
        if (cliente == null)
            return RedirectToAction("Clientes");

        if (string.IsNullOrWhiteSpace(cliente.Nome)) {
            TempData["Erro"] = "Nome é obrigatório.";
            return RedirectToAction("Clientes");
        }

        if (string.IsNullOrWhiteSpace(cliente.Email)) {
            TempData["Erro"] = "Email é obrigatório.";
            return RedirectToAction("Clientes");
        }

        if (string.IsNullOrWhiteSpace(cliente.Senha)) {
            TempData["Erro"] = "Senha é obrigatória.";
            return RedirectToAction("Clientes");
        }

        if (!string.IsNullOrEmpty(cliente.CPF)) {
            cliente.CPF = cliente.CPF.Replace(".", "").Replace("-", "");
        }

        cliente.Senha = BCrypt.Net.BCrypt.HashPassword(cliente.Senha);
        cliente.IsAtivo = true;

        _context.Clientes.Add(cliente);
        _context.SaveChanges();

        TempData["Sucesso"] = "Cliente criado com sucesso!";
        return RedirectToAction("Clientes");
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

    public IActionResult Livros() {
        ViewBag.CategoriasDisponiveis = _context.Categorias
            .OrderBy(c => c.Nome)
            .ToList();

        var livros = _livroService.Listar();
        return View(livros);
    }

    [HttpPost]
    public IActionResult CriarLivro(Livro livro, IFormFile ImagemArquivo, int[] categoriasIds) {
        livro.Preco = ObterDecimalFormulario("Preco", livro.Preco);
        livro.Altura = ObterDecimalFormulario("Altura", livro.Altura);
        livro.Largura = ObterDecimalFormulario("Largura", livro.Largura);
        livro.Peso = ObterDecimalFormulario("Peso", livro.Peso);
        livro.Profundidade = ObterDecimalFormulario("Profundidade", livro.Profundidade);
        livro.Categorias = categoriasIds == null || categoriasIds.Length == 0
            ? new List<Categoria>()
            : _context.Categorias
                .Where(c => categoriasIds.Contains(c.Id))
                .ToList();

        if (ImagemArquivo != null && ImagemArquivo.Length > 0) {
            var nomeArquivo = Guid.NewGuid() + Path.GetExtension(ImagemArquivo.FileName);
            var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/assets/img");

            if (!Directory.Exists(pasta)) {
                Directory.CreateDirectory(pasta);
            }

            var caminho = Path.Combine(pasta, nomeArquivo);

            using (var stream = new FileStream(caminho, FileMode.Create)) {
                ImagemArquivo.CopyTo(stream);
            }

            livro.ImagemUrl = "/assets/img/" + nomeArquivo;
            ModelState.Remove("ImagemUrl");
        }

        ModelState.Remove("Estoque");

        if (!ModelState.IsValid) {
            var erros = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .Select(x => $"{ObterNomeCampoLivro(x.Key)}: {string.Join(" ", x.Value!.Errors.Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)))}".Trim())
                .ToList();

            TempData["Erro"] = erros.Any()
                ? $"Dados inválidos em: {string.Join(" | ", erros)}"
                : "Dados inválidos!";
            return RedirectToAction("Livros");
        }

        if (livro.Categorias == null || !livro.Categorias.Any()) {
            TempData["Erro"] = "Selecione pelo menos uma categoria para o livro.";
            return RedirectToAction("Livros");
        }

        _livroService.Criar(livro);
        TempData["Sucesso"] = "Livro cadastrado com sucesso!";
        return RedirectToAction("Livros");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditarCategoriasLivro(int livroId, int[] categoriasIds) {
        var livro = _context.Livros
            .Include(l => l.Categorias)
            .FirstOrDefault(l => l.Id == livroId);

        if (livro == null) {
            TempData["Erro"] = "Livro nao encontrado.";
            return RedirectToAction("Livros");
        }

        var categoriasSelecionadas = (categoriasIds ?? Array.Empty<int>())
            .Distinct()
            .ToArray();

        if (!categoriasSelecionadas.Any()) {
            TempData["Erro"] = "Selecione pelo menos uma categoria para o livro.";
            return RedirectToAction("Livros");
        }

        var categorias = _context.Categorias
            .Where(c => categoriasSelecionadas.Contains(c.Id))
            .ToList();

        livro.Categorias ??= new List<Categoria>();
        livro.Categorias.Clear();

        foreach (var categoria in categorias) {
            livro.Categorias.Add(categoria);
        }

        _context.SaveChanges();
        TempData["Sucesso"] = $"Categorias do livro \"{livro.Titulo}\" atualizadas com sucesso!";
        return RedirectToAction("Livros");
    }

    public IActionResult Estoque(string? busca, string? status) {
        ViewBag.EstoqueBusca = busca ?? string.Empty;
        ViewBag.EstoqueStatus = string.IsNullOrWhiteSpace(status) ? "todos" : status;

        var estoques = _estoqueService.Listar();
        return View(estoques);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GerarHistoricoAnaliseVendas(int meses = 13, CancellationToken cancellationToken = default) {
        var resultado = await _adminSalesHistorySeedService.GenerateAsync(meses, cancellationToken);

        if (resultado.Succeeded) {
            TempData["Sucesso"] = resultado.Message;
        }
        else {
            TempData["Erro"] = resultado.Message;
        }

        return RedirectToAction(nameof(AnaliseVendas));
    }

        [HttpGet]
    public async Task<IActionResult> AnaliseVendas(DateTime? dataInicio, DateTime? dataFim, int[]? categoriasIds, string? agrupamento, CancellationToken cancellationToken) {
        var analysis = await _salesAnalysisService.BuildAsync(
            new SalesAnalysisQuery {
                DataInicio = dataInicio,
                DataFim = dataFim,
                CategoriasIds = categoriasIds,
                Agrupamento = agrupamento
            },
            cancellationToken);

        return View(AdminAnaliseVendasViewModelMapper.Map(analysis));
    }

    private static string NormalizarAgrupamentoAnalise(string? agrupamento, DateTime inicio, DateTime fim) {
        var valorInformado = (agrupamento ?? string.Empty).Trim().ToLowerInvariant();
        if (valorInformado is "diario" or "semanal" or "mensal") {
            return valorInformado;
        }

        var intervaloDias = (fim - inicio).TotalDays;
        if (intervaloDias > 180) {
            return "mensal";
        }

        if (intervaloDias > 45) {
            return "semanal";
        }

        return "diario";
    }

    private static DateTime ObterInicioPeriodoAnalise(DateTime data, string agrupamento) {
        return agrupamento switch {
            "mensal" => new DateTime(data.Year, data.Month, 1),
            "semanal" => data.Date.AddDays(-((7 + (int)data.DayOfWeek - (int)DayOfWeek.Monday) % 7)),
            _ => data.Date
        };
    }

    private static IEnumerable<DateTime> GerarPeriodosAnalise(DateTime inicio, DateTime fim, string agrupamento) {
        var atual = ObterInicioPeriodoAnalise(inicio, agrupamento);
        var ultimo = ObterInicioPeriodoAnalise(fim, agrupamento);

        while (atual <= ultimo) {
            yield return atual;
            atual = agrupamento switch {
                "mensal" => atual.AddMonths(1),
                "semanal" => atual.AddDays(7),
                _ => atual.AddDays(1)
            };
        }
    }

    private static string FormatarRotuloPeriodoAnalise(DateTime data, string agrupamento) {
        return agrupamento switch {
            "mensal" => data.ToString("MM/yyyy"),
            "semanal" => $"{data:dd/MM} - {data.AddDays(6):dd/MM}",
            _ => data.ToString("dd/MM")
        };
    }

    [HttpGet]
        public async Task<IActionResult> Pedidos(string? busca, string? status, int pagina = 1, CancellationToken cancellationToken = default) {
        var result = await _adminOrdersService.BuildAsync(
            new AdminOrdersQuery {
                Busca = busca,
                Status = status,
                Pagina = pagina
            },
            cancellationToken);

        return View(AdminPedidosViewModelMapper.Map(result));
    }

        [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarStatusPedido(int pedidoId, string novoStatus, CancellationToken cancellationToken = default) {
        var result = await _adminOrdersService.UpdateStatusAsync(
            new AdminOrderStatusUpdateCommand {
                PedidoId = pedidoId,
                NovoStatus = novoStatus
            },
            cancellationToken);

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Pedidos");
    }

    [HttpGet]
        public async Task<IActionResult> Trocas(string? busca, string? status, int paginaTrocas = 1, int paginaCupons = 1, CancellationToken cancellationToken = default) {
        var result = await _adminExchangesService.BuildAsync(
            new AdminExchangesQuery {
                Busca = busca,
                Status = status,
                PaginaTrocas = paginaTrocas,
                PaginaCupons = paginaCupons
            },
            cancellationToken);

        return View(AdminTrocasViewModelMapper.Map(result));
    }

        [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalisarTroca(int trocaId, string decisao, string? observacaoAdmin, CancellationToken cancellationToken = default) {
        var result = await _adminExchangesService.AnalyzeAsync(
            new AdminExchangeAnalysisCommand {
                TrocaId = trocaId,
                Decisao = decisao,
                ObservacaoAdmin = observacaoAdmin
            },
            cancellationToken);

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Trocas");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarRecebimentoTroca(int trocaId, bool retornarAoEstoque, string? observacaoAdmin, decimal? valorCupom, CancellationToken cancellationToken = default) {
        var result = await _adminExchangesService.ConfirmReceiptAsync(
            new AdminExchangeReceiptCommand {
                TrocaId = trocaId,
                RetornarAoEstoque = retornarAoEstoque,
                ObservacaoAdmin = observacaoAdmin,
                ValorCupom = ObterDecimalFormulario("valorCupom", valorCupom ?? 0)
            },
            cancellationToken);

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Trocas");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
        public async Task<IActionResult> GerarCupomDesconto(decimal? valor, string? destinatario, int? clienteId, CancellationToken cancellationToken = default) {
        var result = await _adminExchangesService.GeneratePromotionalCouponAsync(
            new AdminPromotionalCouponCommand {
                Valor = ObterDecimalFormulario("valor", valor ?? 0),
                Destinatario = destinatario,
                ClienteId = clienteId
            },
            cancellationToken);

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Trocas");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesativarCupomDesconto(int id, CancellationToken cancellationToken = default) {
        var result = await _adminExchangesService.DeactivateCouponAsync(
            new AdminCouponDeactivationCommand {
                CupomId = id
            },
            cancellationToken);

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Trocas");
    }

    private decimal ObterDecimalFormulario(string campo, decimal valorPadrao) {
        if (Request?.Form == null || !Request.Form.ContainsKey(campo)) {
            return valorPadrao;
        }

        var valorBruto = Request.Form[campo].ToString();
        if (string.IsNullOrWhiteSpace(valorBruto)) {
            return valorPadrao;
        }

        var normalizado = valorBruto.Trim().Replace(".", string.Empty).Replace(',', '.');

        if (decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorNormalizado)) {
            return valorNormalizado;
        }

        if (decimal.TryParse(valorBruto, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var valorPtBr)) {
            return valorPtBr;
        }

        if (decimal.TryParse(valorBruto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorInvariant)) {
            return valorInvariant;
        }

        return valorPadrao;
    }

    private string ObterNomeCampoLivro(string campo) {
        return campo switch {
            nameof(Livro.Titulo) => "Título",
            nameof(Livro.Autor) => "Autor",
            nameof(Livro.Ano) => "Ano",
            nameof(Livro.Editora) => "Editora",
            nameof(Livro.Edicao) => "Edição",
            nameof(Livro.ISBN) => "ISBN",
            nameof(Livro.CodigoBarras) => "Código de barras",
            nameof(Livro.NumeroPaginas) => "Páginas",
            nameof(Livro.Sinopse) => "Sinopse",
            nameof(Livro.Altura) => "Altura",
            nameof(Livro.Largura) => "Largura",
            nameof(Livro.Peso) => "Peso",
            nameof(Livro.Profundidade) => "Profundidade",
            nameof(Livro.Preco) => "Preço",
            nameof(Livro.ImagemUrl) => "Imagem",
            _ => string.IsNullOrWhiteSpace(campo) ? "Campo desconhecido" : campo
        };
    }
}









