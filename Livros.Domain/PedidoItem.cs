namespace Livros.Domain {
    public class PedidoItem {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;
        public int LivroId { get; set; }
        public Livro Livro { get; set; } = null!;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
    }
}
