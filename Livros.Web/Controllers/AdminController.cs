using Livros.Domain;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Services;
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

        ViewBag.PaginaAtual = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalClientes / pageSize);

        return View(clientes);
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

    public IActionResult Estoque() {
        var estoques = _estoqueService.Listar();
        return View(estoques);
    }

    [HttpPost]
    public IActionResult AdicionarEstoque(int livroId, int quantidade) {
        _estoqueService.AdicionarEstoque(livroId, quantidade);
        return RedirectToAction("Estoque");
    }

    [HttpPost]
    public IActionResult AjustarEstoque(int livroId, int quantidade) {
        _estoqueService.AjustarEstoque(livroId, quantidade);
        return RedirectToAction("Estoque");
    }

    [HttpGet]
    public IActionResult Trocas(string? busca, string? status) {
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
            query = query.Where(t => t.Status == status);
        }

        var trocas = query
            .OrderByDescending(t => t.DataSolicitacao)
            .ToList();

        var vm = new AdminTrocasViewModel {
            Busca = busca,
            StatusFiltro = status,
            Trocas = trocas.Select(t => new AdminTrocaItemViewModel {
                Id = t.Id,
                Codigo = t.Codigo,
                PedidoId = t.PedidoId,
                ClienteNome = t.Cliente?.Nome ?? string.Empty,
                LivroTitulo = t.PedidoItem?.Livro?.Titulo ?? "Livro",
                Motivo = t.Motivo,
                ObservacaoCliente = t.ObservacaoCliente,
                ObservacaoAdmin = t.ObservacaoAdmin,
                Status = t.Status,
                DataSolicitacao = t.DataSolicitacao,
                ValorSugeridoCupom = CalcularValorCupomTroca(t.PedidoItem, t.Pedido),
                CodigoCupom = t.CupomDesconto?.Codigo
            }).ToList(),
            CuponsRecentes = _context.CuponsDesconto
                .OrderByDescending(c => c.DataCriacao)
                .Take(8)
                .ToList(),
            Cupons = _context.CuponsDesconto
                .Include(c => c.Cliente)
                .OrderByDescending(c => c.DataCriacao)
                .Select(c => new AdminCupomItemViewModel {
                    Id = c.Id,
                    Codigo = c.Codigo,
                    Valor = c.Valor,
                    Tipo = c.Tipo,
                    Status = !c.IsAtivo ? "Inativo" : c.DataUtilizacao.HasValue ? "Utilizado" : "Ativo",
                    DataCriacao = c.DataCriacao,
                    DataUtilizacao = c.DataUtilizacao,
                    ClienteNome = c.Cliente != null ? c.Cliente.Nome : null,
                    PedidoId = c.PedidoId,
                    PodeDesativar = c.IsAtivo && !c.DataUtilizacao.HasValue && c.Tipo == "PROMOCIONAL"
                }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AnalisarTroca(int trocaId, string decisao, string? observacaoAdmin, decimal? valorCupom) {
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

        if (troca.Status != "Solicitado") {
            TempData["Erro"] = "Esta solicitação já foi analisada.";
            return RedirectToAction("Trocas");
        }

        troca.ObservacaoAdmin = observacaoAdmin?.Trim();
        troca.DataAnalise = DateTime.Now;

        if (string.Equals(decisao, "aprovar", StringComparison.OrdinalIgnoreCase)) {
            var valorCupomNormalizado = CalcularValorCupomTroca(troca.PedidoItem, troca.Pedido);

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

            troca.Status = "Aprovado";
            troca.CupomDescontoId = cupom.Id;

            _context.SaveChanges();
            TempData["Sucesso"] = $"Troca aprovada e cupom {cupom.Codigo} gerado com sucesso.";
            return RedirectToAction("Trocas");
        }

        troca.Status = "Recusado";
        _context.SaveChanges();
        TempData["Sucesso"] = "Solicitação de troca recusada com sucesso.";
        return RedirectToAction("Trocas");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GerarCupomDesconto(decimal? valor) {
        var valorNormalizado = ObterDecimalFormulario("valor", valor ?? 0);
        if (valorNormalizado <= 0) {
            TempData["Erro"] = "Informe um valor válido para gerar o cupom promocional.";
            return RedirectToAction("Trocas");
        }

        var cupom = new CupomDesconto {
            Codigo = GerarCodigoCupom("PROMO"),
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


