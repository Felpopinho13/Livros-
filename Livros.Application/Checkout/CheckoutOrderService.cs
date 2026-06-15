using Livros.Domain;
namespace Livros.Application.Checkout {
    public sealed class CheckoutOrderService {
        public Pedido Build(CheckoutOrderBuildRequest request) {
            var pedido = new Pedido {
                ClienteId = request.ClienteId,
                EnderecoId = request.EnderecoId,
                Data = DateTime.Now,
                Total = request.Total,
                TipoEntrega = request.TipoEntrega,
                DataEntregaPrevista = request.DataEntregaPrevista,
                Status = "APROVADA",
                Itens = new List<PedidoItem>(),
                Pagamentos = new List<Pagamento>()
            };
            foreach (var item in request.Itens) {
                pedido.Itens.Add(new PedidoItem {
                    LivroId = item.LivroId,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario
                });
            }
            return pedido;
        }
    }
}
