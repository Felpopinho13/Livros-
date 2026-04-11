using Livros.Domain;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Services;
using Livros.Web.Helpers;
using Livros.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

public class AdminController : Controller {
    private readonly AppDbContext _context;
    private readonly LivroService _livroService;
    private readonly EstoqueService _estoqueService;

    public AdminController(AppDbContext context, LivroService livroService, EstoqueService estoqueService) {
        _context = context;
        _livroService = livroService;
        _estoqueService = estoqueService;
    }

    public IActionResult Dashboard() {
        return View();
    }

    public IActionResult Clientes(string busca, string status, string admin, int pagina = 1) {
        int pageSize = 10;

        var query = _context.Clientes.AsQueryable();

        if (!string.IsNullOrEmpty(busca)) {
            query = query.Where(c =>
                c.Nome.Contains(busca) ||
                c.Email.Contains(busca));
        }

        if (!string.IsNullOrEmpty(status)) {
            if (status == "ativo")
                query = query.Where(c => c.IsAtivo);
            else if (status == "inativo")
                query = query.Where(c => !c.IsAtivo);
        }

        if (!string.IsNullOrEmpty(admin)) {
            bool isAdmin = bool.Parse(admin);
            query = query.Where(c => c.IsAdmin == isAdmin);
        }

        var totalClientes = query.Count();

        var clientes = query
            .OrderBy(c => c.Id)
            .Skip((pagina - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var clienteIds = clientes.Select(c => c.Id).ToList();
        var statusElegiveisRanking = new[] {
            "PAGAMENTO APROVADO",
            "EM SEPARACAO",
            "ENVIADO",
            "ENTREGUE"
        };
        var totaisRankingPorCliente = _context.Pedidos
            .Where(p => clienteIds.Contains(p.ClienteId) && statusElegiveisRanking.Contains(p.Status))
            .GroupBy(p => p.ClienteId)
            .Select(g => new {
                ClienteId = g.Key,
                Total = g.Sum(p => p.Total)
            })
            .ToDictionary(x => x.ClienteId, x => decimal.Round(x.Total, 2));

        ViewBag.RankingsPorCliente = clientes.ToDictionary(
            c => c.Id,
            c => ClienteRankingHelper.ObterRanking(
                totaisRankingPorCliente.TryGetValue(c.Id, out var total) ? total : 0m));

        ViewBag.PaginaAtual = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalClientes / pageSize);

        return View(clientes);
    }

    [HttpGet]
    public IActionResult ClienteTransacoes(int id) {
        var cliente = _context.Clientes
            .FirstOrDefault(c => c.Id == id);

        if (cliente == null) {
            TempData["Erro"] = "Cliente nao encontrado.";
            return RedirectToAction("Clientes");
        }

        var pedidos = _context.Pedidos
            .Where(p => p.ClienteId == id)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Livro)
            .Include(p => p.Pagamentos)
            .OrderByDescending(p => p.Data)
            .ToList();

        var pagamentos = pedidos
            .SelectMany(p => p.Pagamentos.Select(pg => new AdminClientePagamentoTransacaoViewModel {
                PedidoId = p.Id,
                Metodo = pg.Metodo,
                Valor = pg.Valor,
                Status = pg.Status
            }))
            .OrderByDescending(pg => pg.PedidoId)
            .ToList();

        var trocas = _context.Trocas
            .Where(t => t.ClienteId == id)
            .Include(t => t.PedidoItem)
                .ThenInclude(i => i.Livro)
            .OrderByDescending(t => t.DataSolicitacao)
            .ToList();

        var cupons = _context.CuponsDesconto
            .Where(c => c.ClienteId == id)
            .OrderByDescending(c => c.DataCriacao)
            .ToList();
        var ranking = ClienteRankingHelper.ObterRanking(ClienteRankingHelper.CalcularValorElegivel(pedidos));

        var vm = new AdminClienteTransacoesViewModel {
            ClienteId = cliente.Id,
            ClienteNome = cliente.Nome,
            ClienteEmail = cliente.Email,
            TotalPedidos = pedidos.Count,
            ValorTotalCompras = pedidos.Sum(p => p.Total),
            TotalPagamentos = pagamentos.Count,
            TotalTrocas = trocas.Count,
            TotalCupons = cupons.Count,
            RankingNome = ranking.Nome,
            RankingCssClass = ranking.CssClass,
            ValorElegivelRanking = ranking.ValorElegivel,
            ProximoMarcoRanking = ranking.ProximoMarco,
            ProximoRankingNome = ranking.ProximoNome,
            Pedidos = pedidos.Select(p => new AdminClientePedidoTransacaoViewModel {
                PedidoId = p.Id,
                Data = p.Data,
                Status = p.Status,
                Total = p.Total,
                ResumoItens = MontarResumoItensPedido(p)
            }).ToList(),
            Pagamentos = pagamentos,
            Trocas = trocas.Select(t => new AdminClienteTrocaTransacaoViewModel {
                Codigo = t.Codigo,
                PedidoId = t.PedidoId,
                LivroTitulo = t.PedidoItem?.Livro?.Titulo ?? "Livro",
                Status = t.Status,
                Data = t.DataSolicitacao
            }).ToList(),
            Cupons = cupons.Select(c => new AdminClienteCupomTransacaoViewModel {
                Codigo = c.Codigo,
                Tipo = c.Tipo,
                Valor = c.Valor,
                Status = c.DataUtilizacao.HasValue ? "Utilizado" : c.IsAtivo ? "Ativo" : "Inativo",
                DataCriacao = c.DataCriacao,
                PedidoId = c.PedidoId
            }).ToList()
        };

        return View(vm);
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
        var livros = _livroService.Listar();
        return View(livros);
    }

    [HttpPost]
    public IActionResult CriarLivro(Livro livro, IFormFile ImagemArquivo) {
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

        _livroService.Criar(livro);
        TempData["Sucesso"] = "Livro cadastrado com sucesso!";
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
    public IActionResult AdicionarEstoque(int livroId, int quantidade, string? busca, string? status) {
        _estoqueService.AdicionarEstoque(livroId, quantidade);
        return RedirectToAction("Estoque", new {
            busca,
            status
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AjustarEstoque(int livroId, int quantidade, string? busca, string? status) {
        _estoqueService.AjustarEstoque(livroId, quantidade);
        return RedirectToAction("Estoque", new {
            busca,
            status
        });
    }

    [HttpGet]
    public IActionResult Pedidos(string? busca, string? status, int pagina = 1) {
        const int pageSize = 10;
        var query = _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Endereco)
                .ThenInclude(e => e.Cidade)
                    .ThenInclude(c => c.Estado)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Livro)
            .Include(p => p.Pagamentos)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca)) {
            var buscaNormalizada = busca.Trim();
            query = query.Where(p =>
                p.Id.ToString().Contains(buscaNormalizada) ||
                p.Cliente.Nome.Contains(buscaNormalizada) ||
                p.Cliente.Email.Contains(buscaNormalizada) ||
                p.Itens.Any(i => i.Livro.Titulo.Contains(buscaNormalizada)));
        }

        if (!string.IsNullOrWhiteSpace(status)) {
            query = query.Where(p => p.Status == status);
        }

        var totalPedidos = query.Count();
        var pedidos = query
            .OrderByDescending(p => p.Data)
            .Skip((pagina - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pedidoIds = pedidos.Select(p => p.Id).ToList();
        var trocasPorPedido = _context.Trocas
            .Where(t => pedidoIds.Contains(t.PedidoId))
            .GroupBy(t => t.PedidoId)
            .ToDictionary(g => g.Key, g => g.Count());

        var vm = new AdminPedidosViewModel {
            Busca = busca,
            StatusFiltro = status,
            PaginaAtual = pagina,
            TotalPaginas = Math.Max(1, (int)Math.Ceiling(totalPedidos / (double)pageSize)),
            Pedidos = pedidos.Select(p => new AdminPedidoItemViewModel {
                PedidoId = p.Id,
                Data = p.Data,
                ClienteNome = p.Cliente?.Nome ?? string.Empty,
                ClienteEmail = p.Cliente?.Email ?? string.Empty,
                Total = p.Total,
                Status = p.Status,
                StatusPagamento = ObterStatusPagamentoPedido(p),
                ResumoItens = MontarResumoItensPedido(p),
                QuantidadeItens = p.Itens.Count,
                QuantidadeLivros = p.Itens.Sum(i => i.Quantidade),
                Destino = MontarDestinoPedido(p),
                EstoqueBaixado = StatusExigeBaixaEstoque(p.Status),
                TemTroca = trocasPorPedido.ContainsKey(p.Id),
                QuantidadeTrocas = trocasPorPedido.TryGetValue(p.Id, out var quantidadeTrocas) ? quantidadeTrocas : 0,
                ProximosStatus = ObterProximosStatusPedido(p.Status).ToList()
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AtualizarStatusPedido(int pedidoId, string novoStatus) {
        var pedido = _context.Pedidos
            .Include(p => p.Itens)
                .ThenInclude(i => i.Livro)
            .Include(p => p.Pagamentos)
            .FirstOrDefault(p => p.Id == pedidoId);

        if (pedido == null) {
            TempData["Erro"] = "Pedido nao encontrado.";
            return RedirectToAction("Pedidos");
        }

        if (string.IsNullOrWhiteSpace(novoStatus)) {
            TempData["Erro"] = "Selecione um novo status para o pedido.";
            return RedirectToAction("Pedidos");
        }

        var statusAtual = pedido.Status ?? string.Empty;
        var proximosStatus = ObterProximosStatusPedido(statusAtual).ToList();
        if (!proximosStatus.Contains(novoStatus)) {
            TempData["Erro"] = "A transicao de status informada nao e valida para este pedido.";
            return RedirectToAction("Pedidos");
        }

        var estoqueEstaBaixado = StatusExigeBaixaEstoque(statusAtual);
        var estoqueDeveFicarBaixado = StatusExigeBaixaEstoque(novoStatus);

        if (!estoqueEstaBaixado && estoqueDeveFicarBaixado) {
            var erroBaixa = TentarBaixarEstoquePedido(pedido);
            if (!string.IsNullOrWhiteSpace(erroBaixa)) {
                TempData["Erro"] = erroBaixa;
                return RedirectToAction("Pedidos");
            }
        }
        else if (estoqueEstaBaixado && !estoqueDeveFicarBaixado) {
            ReporEstoquePedido(pedido);
        }

        pedido.Status = novoStatus;
        AtualizarStatusPagamentosPedido(pedido, novoStatus);
        _context.SaveChanges();

        TempData["Sucesso"] = $"Pedido #{pedido.Id} atualizado para {novoStatus}.";
        return RedirectToAction("Pedidos");
    }

    [HttpGet]
    public IActionResult Trocas(string? busca, string? status, int paginaTrocas = 1, int paginaCupons = 1) {
        const int pageSize = 10;
        var query = _context.Trocas
            .Include(t => t.Cliente)
            .Include(t => t.Pedido)
            .Include(t => t.PedidoItem)
                .ThenInclude(i => i.Livro)
            .Include(t => t.CupomDesconto)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca)) {
            var buscaNormalizada = busca.Trim();
            query = query.Where(t =>
                t.Codigo.Contains(buscaNormalizada) ||
                t.Cliente.Nome.Contains(buscaNormalizada) ||
                t.PedidoId.ToString().Contains(buscaNormalizada) ||
                t.PedidoItem.Livro.Titulo.Contains(buscaNormalizada));
        }

        if (!string.IsNullOrWhiteSpace(status)) {
            if (string.Equals(status, "Autorizada", StringComparison.OrdinalIgnoreCase)) {
                query = query.Where(t =>
                    t.Status == "Autorizada" ||
                    (t.Status == "Aprovado" && !t.CupomDescontoId.HasValue));
            }
            else if (string.Equals(status, "Recebida", StringComparison.OrdinalIgnoreCase)) {
                query = query.Where(t =>
                    t.Status == "Recebida" ||
                    (t.Status == "Aprovado" && t.CupomDescontoId.HasValue));
            }
            else {
                query = query.Where(t => t.Status == status);
            }
        }

        var totalTrocas = query.Count();
        var trocas = query
            .OrderByDescending(t => t.DataSolicitacao)
            .Skip((paginaTrocas - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var cuponsQuery = _context.CuponsDesconto
            .Include(c => c.Cliente)
            .OrderByDescending(c => c.DataCriacao);
        var totalCupons = cuponsQuery.Count();
        var cuponsPagina = cuponsQuery
            .Skip((paginaCupons - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var vm = new AdminTrocasViewModel {
            Busca = busca,
            StatusFiltro = status,
            PaginaTrocasAtual = paginaTrocas,
            TotalPaginasTrocas = Math.Max(1, (int)Math.Ceiling(totalTrocas / (double)pageSize)),
            PaginaCuponsAtual = paginaCupons,
            TotalPaginasCupons = Math.Max(1, (int)Math.Ceiling(totalCupons / (double)pageSize)),
            Trocas = trocas.Select(t => new AdminTrocaItemViewModel {
                Id = t.Id,
                Codigo = t.Codigo,
                PedidoId = t.PedidoId,
                ClienteNome = t.Cliente?.Nome ?? string.Empty,
                LivroTitulo = t.PedidoItem?.Livro?.Titulo ?? "Livro",
                Motivo = t.Motivo,
                ObservacaoCliente = t.ObservacaoCliente,
                ObservacaoAdmin = t.ObservacaoAdmin,
                Status = ObterStatusTrocaExibicao(t),
                DataSolicitacao = t.DataSolicitacao,
                DataRecebimento = t.DataRecebimento,
                RetornarAoEstoque = t.RetornarAoEstoque,
                ValorSugeridoCupom = CalcularValorCupomTroca(t.PedidoItem, t.Pedido),
                ValorCupomGerado = t.CupomDesconto?.Valor,
                CodigoCupom = t.CupomDesconto?.Codigo,
                PodeAnalisar = TrocaEstaSolicitada(t),
                PodeConfirmarRecebimento = TrocaEstaAutorizada(t)
            }).ToList(),
            CuponsRecentes = _context.CuponsDesconto
                .OrderByDescending(c => c.DataCriacao)
                .Take(8)
                .ToList(),
            Cupons = cuponsPagina
                .Select(c => new AdminCupomItemViewModel {
                    Id = c.Id,
                    Codigo = c.Codigo,
                    Valor = c.Valor,
                    Tipo = c.Tipo,
                    Status = !c.IsAtivo ? "Inativo" : c.DataUtilizacao.HasValue ? "Utilizado" : "Ativo",
                    Publico = c.ClienteId.HasValue ? "Cliente específico" : "Código manual",
                    DataCriacao = c.DataCriacao,
                    DataUtilizacao = c.DataUtilizacao,
                    ClienteNome = c.Cliente != null ? c.Cliente.Nome : null,
                    PedidoId = c.PedidoId,
                    PodeDesativar = c.IsAtivo && !c.DataUtilizacao.HasValue && c.Tipo == "PROMOCIONAL"
                }).ToList(),
            ClientesAtivos = _context.Clientes
                .Where(c => c.IsAtivo)
                .OrderBy(c => c.Nome)
                .Select(c => new AdminCupomClienteOptionViewModel {
                    Id = c.Id,
                    Nome = c.Nome,
                    Email = c.Email
                })
                .ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AnalisarTroca(int trocaId, string decisao, string? observacaoAdmin) {
        var troca = _context.Trocas
            .Include(t => t.Pedido)
            .Include(t => t.PedidoItem)
                .ThenInclude(i => i.Livro)
            .Include(t => t.CupomDesconto)
            .FirstOrDefault(t => t.Id == trocaId);

        if (troca == null) {
            TempData["Erro"] = "Solicitação de troca não encontrada.";
            return RedirectToAction("Trocas");
        }

        if (!TrocaEstaSolicitada(troca)) {
            TempData["Erro"] = "Esta solicitação já foi analisada.";
            return RedirectToAction("Trocas");
        }

        troca.ObservacaoAdmin = observacaoAdmin?.Trim();
        troca.DataAnalise = DateTime.Now;

        if (string.Equals(decisao, "aprovar", StringComparison.OrdinalIgnoreCase)) {
            troca.Status = "Autorizada";

            _context.SaveChanges();
            TempData["Sucesso"] = "Troca autorizada com sucesso. O cupom sera gerado somente apos o recebimento do item devolvido.";
            return RedirectToAction("Trocas");
        }

        troca.Status = "Recusado";
        _context.SaveChanges();
        TempData["Sucesso"] = "Solicitação de troca recusada com sucesso.";
        return RedirectToAction("Trocas");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmarRecebimentoTroca(int trocaId, bool retornarAoEstoque, string? observacaoAdmin, decimal? valorCupom) {
        var troca = _context.Trocas
            .Include(t => t.Pedido)
            .Include(t => t.PedidoItem)
                .ThenInclude(i => i.Livro)
            .Include(t => t.CupomDesconto)
            .FirstOrDefault(t => t.Id == trocaId);

        if (troca == null) {
            TempData["Erro"] = "Solicitação de troca não encontrada.";
            return RedirectToAction("Trocas");
        }

        if (!TrocaEstaAutorizada(troca)) {
            TempData["Erro"] = "Somente trocas autorizadas podem ter o recebimento confirmado.";
            return RedirectToAction("Trocas");
        }

        var valorSugerido = CalcularValorCupomTroca(troca.PedidoItem, troca.Pedido);
        var valorInformado = ObterDecimalFormulario("valorCupom", valorCupom ?? 0);
        var valorCupomNormalizado = valorInformado > 0 ? valorInformado : valorSugerido;

        if (valorCupomNormalizado <= 0) {
            TempData["Erro"] = "Informe um valor válido para o cupom de troca.";
            return RedirectToAction("Trocas");
        }

        troca.ObservacaoAdmin = observacaoAdmin?.Trim();
        troca.DataRecebimento = DateTime.Now;
        troca.RetornarAoEstoque = retornarAoEstoque;
        troca.Status = "Recebida";

        if (retornarAoEstoque) {
            ReintegrarItemTrocaAoEstoque(troca.PedidoItem);
        }

        if (!troca.CupomDescontoId.HasValue) {
            var cupom = new CupomDesconto {
                Codigo = GerarCodigoCupom("TROCA"),
                Valor = valorCupomNormalizado,
                Tipo = "TROCA",
                IsAtivo = true,
                ClienteId = troca.ClienteId,
                DataCriacao = DateTime.Now
            };

            _context.CuponsDesconto.Add(cupom);
            _context.SaveChanges();

            troca.CupomDescontoId = cupom.Id;
            _context.SaveChanges();

            TempData["Sucesso"] = retornarAoEstoque
                ? $"Recebimento confirmado, item devolvido reintegrado ao estoque e cupom {cupom.Codigo} gerado."
                : $"Recebimento confirmado e cupom {cupom.Codigo} gerado com sucesso.";
            return RedirectToAction("Trocas");
        }

        _context.SaveChanges();
        TempData["Sucesso"] = retornarAoEstoque
            ? "Recebimento confirmado e item devolvido reintegrado ao estoque."
            : "Recebimento confirmado com sucesso.";
        return RedirectToAction("Trocas");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GerarCupomDesconto(decimal? valor, string? destinatario, int? clienteId) {
        var valorNormalizado = ObterDecimalFormulario("valor", valor ?? 0);
        if (valorNormalizado <= 0) {
            TempData["Erro"] = "Informe um valor válido para gerar o cupom promocional.";
            return RedirectToAction("Trocas");
        }

        var codigoBase = GerarCodigoCupom("PROMO");
        var gerarParaTodos = string.Equals(destinatario, "todos", StringComparison.OrdinalIgnoreCase);

        if (gerarParaTodos) {
            var clientesAtivos = _context.Clientes
                .Where(c => c.IsAtivo)
                .Select(c => new { c.Id })
                .ToList();

            if (!clientesAtivos.Any()) {
                TempData["Erro"] = "Nao ha clientes ativos para receber este cupom.";
                return RedirectToAction("Trocas");
            }

            foreach (var cliente in clientesAtivos) {
                _context.CuponsDesconto.Add(new CupomDesconto {
                    Codigo = codigoBase,
                    Valor = valorNormalizado,
                    Tipo = "PROMOCIONAL",
                    IsAtivo = true,
                    ClienteId = cliente.Id,
                    DataCriacao = DateTime.Now
                });
            }

            _context.SaveChanges();
            TempData["Sucesso"] = $"Cupom promocional {codigoBase} gerado para {clientesAtivos.Count} cliente(s).";
            return RedirectToAction("Trocas");
        }

        if (string.Equals(destinatario, "cliente", StringComparison.OrdinalIgnoreCase)) {
            if (!clienteId.HasValue || clienteId.Value <= 0) {
                TempData["Erro"] = "Selecione um cliente valido para vincular o cupom.";
                return RedirectToAction("Trocas");
            }

            var cliente = _context.Clientes
                .FirstOrDefault(c => c.Id == clienteId.Value && c.IsAtivo);

            if (cliente == null) {
                TempData["Erro"] = "Nao foi possivel localizar o cliente selecionado.";
                return RedirectToAction("Trocas");
            }

            var cupomCliente = new CupomDesconto {
                Codigo = codigoBase,
                Valor = valorNormalizado,
                Tipo = "PROMOCIONAL",
                IsAtivo = true,
                ClienteId = cliente.Id,
                DataCriacao = DateTime.Now
            };

            _context.CuponsDesconto.Add(cupomCliente);
            _context.SaveChanges();

            TempData["Sucesso"] = $"Cupom promocional {cupomCliente.Codigo} gerado para {cliente.Nome}.";
            return RedirectToAction("Trocas");
        }

        var cupom = new CupomDesconto {
            Codigo = codigoBase,
            Valor = valorNormalizado,
            Tipo = "PROMOCIONAL",
            IsAtivo = true,
            DataCriacao = DateTime.Now
        };

        _context.CuponsDesconto.Add(cupom);
        _context.SaveChanges();

        TempData["Sucesso"] = $"Cupom promocional {cupom.Codigo} gerado com sucesso.";
        return RedirectToAction("Trocas");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DesativarCupomDesconto(int id) {
        var cupom = _context.CuponsDesconto.FirstOrDefault(c => c.Id == id);

        if (cupom == null) {
            TempData["Erro"] = "Cupom nao encontrado.";
            return RedirectToAction("Trocas");
        }

        if (!cupom.IsAtivo || cupom.DataUtilizacao.HasValue) {
            TempData["Erro"] = "Este cupom nao pode ser desativado manualmente.";
            return RedirectToAction("Trocas");
        }

        if (!string.Equals(cupom.Tipo, "PROMOCIONAL", StringComparison.OrdinalIgnoreCase)) {
            TempData["Erro"] = "Apenas cupons promocionais podem ser desativados manualmente.";
            return RedirectToAction("Trocas");
        }

        cupom.IsAtivo = false;
        _context.SaveChanges();

        TempData["Sucesso"] = $"Cupom {cupom.Codigo} desativado com sucesso.";
        return RedirectToAction("Trocas");
    }
    private string GerarCodigoCupom(string prefixo) {
        return $"{prefixo}-{DateTime.Now:yyyyMMddHHmmss}";
    }

    private static bool TrocaEstaSolicitada(Troca troca) {
        return string.Equals(troca.Status, "Solicitado", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TrocaEstaAutorizada(Troca troca) {
        return string.Equals(troca.Status, "Autorizada", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(troca.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) && !troca.CupomDescontoId.HasValue);
    }

    private static bool TrocaEstaRecebida(Troca troca) {
        return string.Equals(troca.Status, "Recebida", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(troca.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) && troca.CupomDescontoId.HasValue);
    }

    private static string ObterStatusTrocaExibicao(Troca troca) {
        if (TrocaEstaRecebida(troca)) {
            return "Recebida";
        }

        if (TrocaEstaAutorizada(troca)) {
            return "Autorizada";
        }

        return troca.Status;
    }

    private string ObterStatusPagamentoPedido(Pedido pedido) {
        if (pedido.Pagamentos == null || !pedido.Pagamentos.Any()) {
            return "Sem pagamento";
        }

        if (pedido.Pagamentos.All(p => string.Equals(p.Status, "Cancelado", StringComparison.OrdinalIgnoreCase))) {
            return "Cancelado";
        }

        if (pedido.Pagamentos.All(p => string.Equals(p.Status, "Recusado", StringComparison.OrdinalIgnoreCase))) {
            return "Recusado";
        }

        if (pedido.Pagamentos.All(p => string.Equals(p.Status, "Aprovado", StringComparison.OrdinalIgnoreCase))) {
            return "Aprovado";
        }

        return "Pendente";
    }

    private string MontarResumoItensPedido(Pedido pedido) {
        var itemPrincipal = pedido.Itens.FirstOrDefault();
        if (itemPrincipal == null) {
            return "Pedido sem itens";
        }

        if (pedido.Itens.Count == 1) {
            return itemPrincipal.Livro?.Titulo ?? "Livro";
        }

        return $"{itemPrincipal.Livro?.Titulo ?? "Livro"} + {pedido.Itens.Count - 1} item(ns)";
    }

    private string MontarDestinoPedido(Pedido pedido) {
        var cidade = pedido.Endereco?.Cidade?.Nome ?? string.Empty;
        var estado = pedido.Endereco?.Cidade?.Estado?.Sigla ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cidade) && string.IsNullOrWhiteSpace(estado)) {
            return "Endereco nao informado";
        }

        return string.IsNullOrWhiteSpace(estado) ? cidade : $"{cidade}/{estado}";
    }

    private static bool StatusExigeBaixaEstoque(string? status) {
        if (string.IsNullOrWhiteSpace(status)) {
            return false;
        }

        return status.Equals("PAGAMENTO APROVADO", StringComparison.OrdinalIgnoreCase)
            || status.Equals("EM SEPARACAO", StringComparison.OrdinalIgnoreCase)
            || status.Equals("ENVIADO", StringComparison.OrdinalIgnoreCase)
            || status.Equals("ENTREGUE", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<string> ObterProximosStatusPedido(string? statusAtual) {
        var status = (statusAtual ?? string.Empty).Trim().ToUpperInvariant();

        return status switch {
            "EM PROCESSAMENTO" => new[] { "PAGAMENTO APROVADO", "PAGAMENTO RECUSADO", "CANCELADO" },
            "PAGAMENTO APROVADO" => new[] { "EM SEPARACAO", "CANCELADO" },
            "EM SEPARACAO" => new[] { "ENVIADO", "CANCELADO" },
            "ENVIADO" => new[] { "ENTREGUE" },
            _ => Array.Empty<string>()
        };
    }

    private string? TentarBaixarEstoquePedido(Pedido pedido) {
        foreach (var item in pedido.Itens) {
            var estoque = _context.Estoques.FirstOrDefault(e => e.LivroId == item.LivroId);
            if (estoque == null) {
                return $"Nao foi encontrado estoque para o livro \"{item.Livro?.Titulo ?? item.LivroId.ToString()}\".";
            }

            if (estoque.Quantidade < item.Quantidade) {
                return $"Estoque insuficiente para o livro \"{item.Livro?.Titulo ?? item.LivroId.ToString()}\". Disponivel: {estoque.Quantidade}.";
            }
        }

        foreach (var item in pedido.Itens) {
            var estoque = _context.Estoques.First(e => e.LivroId == item.LivroId);
            estoque.Quantidade -= item.Quantidade;
        }

        return null;
    }

    private void ReporEstoquePedido(Pedido pedido) {
        foreach (var item in pedido.Itens) {
            var estoque = _context.Estoques.FirstOrDefault(e => e.LivroId == item.LivroId);
            if (estoque == null) {
                estoque = new Estoque {
                    LivroId = item.LivroId,
                    Quantidade = 0
                };
                _context.Estoques.Add(estoque);
            }

            estoque.Quantidade += item.Quantidade;
        }
    }

    private void ReintegrarItemTrocaAoEstoque(PedidoItem? pedidoItem) {
        if (pedidoItem == null) {
            return;
        }

        var estoque = _context.Estoques.FirstOrDefault(e => e.LivroId == pedidoItem.LivroId);
        if (estoque == null) {
            estoque = new Estoque {
                LivroId = pedidoItem.LivroId,
                Quantidade = 0
            };
            _context.Estoques.Add(estoque);
        }

        estoque.Quantidade += pedidoItem.Quantidade;
    }

    private void AtualizarStatusPagamentosPedido(Pedido pedido, string novoStatus) {
        if (pedido.Pagamentos == null || !pedido.Pagamentos.Any()) {
            return;
        }

        var statusPagamento = novoStatus.Trim().ToUpperInvariant() switch {
            "EM PROCESSAMENTO" => "Pendente",
            "PAGAMENTO APROVADO" => "Aprovado",
            "EM SEPARACAO" => "Aprovado",
            "ENVIADO" => "Aprovado",
            "ENTREGUE" => "Aprovado",
            "PAGAMENTO RECUSADO" => "Recusado",
            "CANCELADO" => "Cancelado",
            _ => "Pendente"
        };

        foreach (var pagamento in pedido.Pagamentos) {
            pagamento.Status = statusPagamento;
        }
    }

    private decimal CalcularValorCupomTroca(PedidoItem? pedidoItem, Pedido? pedido) {
        if (pedidoItem == null || pedido == null) {
            return 0;
        }

        var subtotalPedido = _context.PedidoItens
            .Where(i => i.PedidoId == pedido.Id)
            .Sum(i => i.PrecoUnitario * i.Quantidade);
        var descontoPedido = _context.CuponsDesconto
            .Where(c => c.PedidoId == pedido.Id)
            .Sum(c => c.Valor);

        var totalItem = pedidoItem.PrecoUnitario * pedidoItem.Quantidade;
        if (subtotalPedido <= 0) {
            return decimal.Round(totalItem, 2);
        }

        var fretePedido = Math.Max(pedido.Total - subtotalPedido + descontoPedido, 0);
        var proporcaoItem = totalItem / subtotalPedido;
        var freteProporcional = decimal.Round(fretePedido * proporcaoItem, 2);

        return decimal.Round(totalItem + freteProporcional, 2);
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



