using Livros.Domain;

public class AdminTrocaItemViewModel {
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int PedidoId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string LivroTitulo { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? ObservacaoCliente { get; set; }
    public string? ObservacaoAdmin { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DataSolicitacao { get; set; }
    public decimal ValorSugeridoCupom { get; set; }
    public string? CodigoCupom { get; set; }
}

public class AdminCupomItemViewModel {
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Publico { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataUtilizacao { get; set; }
    public string? ClienteNome { get; set; }
    public int? PedidoId { get; set; }
    public bool PodeDesativar { get; set; }
}

public class AdminCupomClienteOptionViewModel {
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AdminTrocasViewModel {
    public string? Busca { get; set; }
    public string? StatusFiltro { get; set; }
    public int PaginaTrocasAtual { get; set; }
    public int TotalPaginasTrocas { get; set; }
    public int PaginaCuponsAtual { get; set; }
    public int TotalPaginasCupons { get; set; }
    public List<AdminTrocaItemViewModel> Trocas { get; set; } = new();
    public List<CupomDesconto> CuponsRecentes { get; set; } = new();
    public List<AdminCupomItemViewModel> Cupons { get; set; } = new();
    public List<AdminCupomClienteOptionViewModel> ClientesAtivos { get; set; } = new();
}
