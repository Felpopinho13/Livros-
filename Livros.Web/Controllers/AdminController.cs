using Livros.Application.AdminBooks;
using Livros.Application.AdminCustomers;
using Livros.Application.AdminExchanges;
using Livros.Application.AdminInventory;
using Livros.Application.AdminOrders;
using Livros.Application.AdminSalesHistory;
using Livros.Application.SalesAnalysis;
using Livros.Domain;
using Livros.Web.Helpers;
using Livros.Web.Models.ViewModels;
using Livros.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

public class AdminController : Controller {
    private readonly AdminCustomersService _adminCustomersService;
    private readonly AdminBooksService _adminBooksService;
    private readonly AdminExchangesService _adminExchangesService;
    private readonly AdminInventoryService _adminInventoryService;
    private readonly AdminOrdersService _adminOrdersService;
    private readonly SalesAnalysisService _salesAnalysisService;
    private readonly AdminSalesHistorySeedService _adminSalesHistorySeedService;
    private readonly BookImageStorageService _bookImageStorageService;

    public AdminController(
        AdminCustomersService adminCustomersService,
        AdminBooksService adminBooksService,
        AdminExchangesService adminExchangesService,
        AdminInventoryService adminInventoryService,
        AdminOrdersService adminOrdersService,
        SalesAnalysisService salesAnalysisService,
        AdminSalesHistorySeedService adminSalesHistorySeedService,
        BookImageStorageService bookImageStorageService) {
        _adminCustomersService = adminCustomersService;
        _adminBooksService = adminBooksService;
        _adminExchangesService = adminExchangesService;
        _adminInventoryService = adminInventoryService;
        _adminOrdersService = adminOrdersService;
        _salesAnalysisService = salesAnalysisService;
        _adminSalesHistorySeedService = adminSalesHistorySeedService;
        _bookImageStorageService = bookImageStorageService;
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
        if (cliente == null) {
            return RedirectToAction("Clientes");
        }

        var result = _adminCustomersService.Create(new AdminCustomerCreateCommand {
            Cliente = cliente
        });

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Clientes");
    }

    [HttpPost]
    public IActionResult DesativarCliente(int id) {
        var result = _adminCustomersService.UpdateStatus(new AdminCustomerStatusCommand {
            ClienteId = id,
            IsAtivo = false
        });

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Clientes");
    }

    [HttpPost]
    public IActionResult AtivarCliente(int id) {
        var result = _adminCustomersService.UpdateStatus(new AdminCustomerStatusCommand {
            ClienteId = id,
            IsAtivo = true
        });

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Clientes");
    }

    [HttpPost]
    public IActionResult EditarClienteAdmin(Cliente cliente) {
        var result = _adminCustomersService.Update(new AdminCustomerUpdateCommand {
            Cliente = cliente
        });

        if (!result.Succeeded && result.Message == "Cliente nao encontrado.") {
            return NotFound();
        }

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Clientes");
    }

    [HttpPost]
    public IActionResult ExcluirClienteAdmin(int id) {
        var result = _adminCustomersService.Delete(new AdminCustomerDeletionCommand {
            ClienteId = id
        });

        if (!result.Succeeded && result.Message == "Cliente nao encontrado.") {
            return NotFound();
        }

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Clientes");
    }

    public IActionResult Livros() {
        var catalog = _adminBooksService.BuildCatalog();
        ViewBag.CategoriasDisponiveis = catalog.CategoriasDisponiveis;
        return View(catalog.Livros);
    }

    [HttpPost]
    public IActionResult CriarLivro(Livro livro, IFormFile ImagemArquivo, int[] categoriasIds) {
        livro.Preco = ObterDecimalFormulario("Preco", livro.Preco);
        livro.Altura = ObterDecimalFormulario("Altura", livro.Altura);
        livro.Largura = ObterDecimalFormulario("Largura", livro.Largura);
        livro.Peso = ObterDecimalFormulario("Peso", livro.Peso);
        livro.Profundidade = ObterDecimalFormulario("Profundidade", livro.Profundidade);

        if (ImagemArquivo != null && ImagemArquivo.Length > 0) {
            livro.ImagemUrl = _bookImageStorageService.Save(ImagemArquivo);
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

        var result = _adminBooksService.Create(new AdminBookCreateCommand {
            Livro = livro,
            CategoriasIds = categoriasIds ?? Array.Empty<int>()
        });

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Livros");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditarCategoriasLivro(int livroId, int[] categoriasIds) {
        var result = _adminBooksService.UpdateCategories(new AdminBookCategoryUpdateCommand {
            LivroId = livroId,
            CategoriasIds = categoriasIds ?? Array.Empty<int>()
        });

        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction("Livros");
    }

    public IActionResult Estoque(string? busca, string? status) {
        ViewBag.EstoqueBusca = busca ?? string.Empty;
        ViewBag.EstoqueStatus = string.IsNullOrWhiteSpace(status) ? "todos" : status;

        var estoques = _adminInventoryService.ListActiveInventory();
        return View(estoques);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AdicionarEstoque(int livroId, int quantidade) {
        var result = _adminInventoryService.AddStock(livroId, quantidade);
        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction(nameof(Estoque));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AjustarEstoque(int livroId, int novoValor) {
        var result = _adminInventoryService.AdjustStock(livroId, novoValor);
        TempData[result.Succeeded ? "Sucesso" : "Erro"] = result.Message;
        return RedirectToAction(nameof(Estoque));
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
