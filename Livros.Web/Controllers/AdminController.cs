using Livros.Application.AdminBooks;
using Livros.Application.AdminCustomers;
using Livros.Application.AdminExchanges;
using Livros.Application.AdminOrders;
using Livros.Application.SalesAnalysis;
using Livros.Domain;
using Livros.Infrastructure.Services;
using Livros.Web.Helpers;
using Livros.Web.Models.ViewModels;
using Livros.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

public class AdminController : Controller {
    private readonly AdminCustomersService _adminCustomersService;
    private readonly AdminBooksService _adminBooksService;
    private readonly AdminExchangesService _adminExchangesService;
    private readonly AdminOrdersService _adminOrdersService;
    private readonly SalesAnalysisService _salesAnalysisService;
    private readonly EstoqueService _estoqueService;
    private readonly AdminSalesHistorySeedService _adminSalesHistorySeedService;

    public AdminController(
        AdminCustomersService adminCustomersService,
        AdminBooksService adminBooksService,
        AdminExchangesService adminExchangesService,
        AdminOrdersService adminOrdersService,
        SalesAnalysisService salesAnalysisService,
        EstoqueService estoqueService,
        AdminSalesHistorySeedService adminSalesHistorySeedService) {
        _adminCustomersService = adminCustomersService;
        _adminBooksService = adminBooksService;
        _adminExchangesService = adminExchangesService;
        _adminOrdersService = adminOrdersService;
        _salesAnalysisService = salesAnalysisService;
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
        if (cliente == null) {
            return RedirectToAction("Clientes");
        }

        if (!string.IsNullOrWhiteSpace(cliente.Senha)) {
            cliente.Senha = BCrypt.Net.BCrypt.HashPassword(cliente.Senha);
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
