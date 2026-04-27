using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Livros.Domain {
    public class Pedido {
        public int Id { get; set; }

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }

        public int EnderecoId { get; set; }
        public Endereco Endereco { get; set; }

        public DateTime Data { get; set; } = DateTime.Now;

        public decimal Total { get; set; }

        public string TipoEntrega { get; set; } = "PADRAO";

        public DateTime? DataEntregaPrevista { get; set; }

        public string Status { get; set; } = "Aguardando Pagamento";

        public List<PedidoItem> Itens { get; set; }
        public List<Pagamento> Pagamentos { get; set; }
    }
}
