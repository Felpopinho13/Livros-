public class MeusPedidosItemViewModel {
    public int PedidoId { get; set; }
    public DateTime Data { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string LivroTitulo { get; set; } = string.Empty;
    public string LivroAutor { get; set; } = string.Empty;
    public string LivroImagemUrl { get; set; } = string.Empty;
    public int QuantidadeItens { get; set; }
    public int QuantidadeLivros { get; set; }
    public int LivroIdPrincipal { get; set; }
}

public class MeusPedidosViewModel {
    public List<MeusPedidosItemViewModel> Pedidos { get; set; } = new();
}
