public class AdminPedidoItemViewModel {
    public int PedidoId { get; set; }
    public DateTime Data { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusPagamento { get; set; } = string.Empty;
    public string ResumoItens { get; set; } = string.Empty;
    public int QuantidadeItens { get; set; }
    public int QuantidadeLivros { get; set; }
    public string Destino { get; set; } = string.Empty;
    public bool EstoqueBaixado { get; set; }
    public bool TemTroca { get; set; }
    public int QuantidadeTrocas { get; set; }
    public List<string> ProximosStatus { get; set; } = new();
}

public class AdminPedidosViewModel {
    public string? Busca { get; set; }
    public string? StatusFiltro { get; set; }
    public int PaginaAtual { get; set; }
    public int TotalPaginas { get; set; }
    public List<AdminPedidoItemViewModel> Pedidos { get; set; } = new();
}
