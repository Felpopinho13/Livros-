using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Livros.Domain {
    public class Pagamento {
        public int Id { get; set; }

        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; }

        public string Metodo { get; set; } // pix, boleto, cartao

        public decimal Valor { get; set; }

        public string Status { get; set; } = "Pendente";
    }
}