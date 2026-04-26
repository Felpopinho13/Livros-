public class PedidoConfirmadoViewModel {
    public int PedidoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TipoEntrega { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string LivroTitulo { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}
