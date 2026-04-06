using System;

namespace Livros.Domain {
    public class CupomDesconto {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = "PROMOCIONAL";
        public bool IsAtivo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public DateTime? DataUtilizacao { get; set; }

        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public int? PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        public int? TrocaId { get; set; }
        public Troca? Troca { get; set; }
    }
}
