using System;

namespace Livros.Domain {
    public class Troca {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;

        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;

        public int PedidoItemId { get; set; }
        public PedidoItem PedidoItem { get; set; } = null!;

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        public string Motivo { get; set; } = string.Empty;
        public string? ObservacaoCliente { get; set; }
        public string? ObservacaoAdmin { get; set; }
        public string Status { get; set; } = "EM TROCA";
        public DateTime DataSolicitacao { get; set; } = DateTime.Now;
        public DateTime? DataAnalise { get; set; }
        public DateTime? DataRecebimento { get; set; }
        public bool? RetornarAoEstoque { get; set; }

        public int? CupomDescontoId { get; set; }
        public CupomDesconto? CupomDesconto { get; set; }
    }
}

