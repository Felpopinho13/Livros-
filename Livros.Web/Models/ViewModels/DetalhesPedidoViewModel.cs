public class DetalhesPedidoItemViewModel {
    public int PedidoItemId { get; set; }
    public int LivroId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string ImagemUrl { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public int? TrocaId { get; set; }
    public string? TrocaStatus { get; set; }
    public string? CodigoCupomTroca { get; set; }
    public decimal? ValorCupomTroca { get; set; }
    public bool PedidoEntregue { get; set; }
    public bool PodeSolicitarTroca => PedidoEntregue && !TrocaId.HasValue;
    public bool AguardandoEntregaParaTroca => !PedidoEntregue && !TrocaId.HasValue;
    public bool TemCupomTroca => !string.IsNullOrWhiteSpace(CodigoCupomTroca) && ValorCupomTroca.HasValue && ValorCupomTroca.Value > 0;
    public decimal TotalItem => PrecoUnitario * Quantidade;
}

public class DetalhesPedidoPagamentoViewModel {
    public string Metodo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DetalhesPedidoViewModel {
    public int PedidoId { get; set; }
    public DateTime Data { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClienteNome { get; set; } = string.Empty;
    public string EnderecoNome { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Complemento { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string CEP { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Frete { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public List<DetalhesPedidoItemViewModel> Itens { get; set; } = new();
    public List<DetalhesPedidoPagamentoViewModel> Pagamentos { get; set; } = new();
}
