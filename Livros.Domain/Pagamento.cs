namespace Livros.Domain {
    public class Pagamento {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;
        public string Metodo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Status { get; set; } = "Pendente";
    }
}
