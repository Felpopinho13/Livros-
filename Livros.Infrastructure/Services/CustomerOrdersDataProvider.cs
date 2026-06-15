using Livros.Application.CustomerOrders;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class CustomerOrdersDataProvider : ICustomerOrdersDataProvider {
        private readonly AppDbContext _context;

        public CustomerOrdersDataProvider(AppDbContext context) {
            _context = context;
        }

        public Pedido? LoadOrderWithItemsAndBooks(int orderId, int customerId) {
            return _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .FirstOrDefault(p => p.Id == orderId && p.ClienteId == customerId);
        }

        public List<Pedido> LoadOrdersByCustomerIdWithItemsAndBooks(int customerId) {
            return _context.Pedidos
                .Where(p => p.ClienteId == customerId)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .OrderByDescending(p => p.Data)
                .ToList();
        }

        public List<Troca> LoadExchangesByOrderIds(IEnumerable<int> orderIds) {
            var ids = orderIds.Distinct().ToList();
            if (!ids.Any()) {
                return new List<Troca>();
            }

            return _context.Trocas
                .Where(t => ids.Contains(t.PedidoId))
                .ToList();
        }

        public Pedido? LoadDetailedOrder(int orderId, int customerId) {
            return _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Endereco)
                    .ThenInclude(e => e.Bairro)
                .Include(p => p.Endereco)
                    .ThenInclude(e => e.Cidade)
                        .ThenInclude(c => c.Estado)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .Include(p => p.Pagamentos)
                .FirstOrDefault(p => p.Id == orderId && p.ClienteId == customerId);
        }

        public List<Troca> LoadExchangesByOrderIdWithCoupon(int orderId) {
            return _context.Trocas
                .Include(t => t.CupomDesconto)
                .Where(t => t.PedidoId == orderId)
                .ToList();
        }

        public List<CupomDesconto> LoadCouponsByOrderId(int orderId) {
            return _context.CuponsDesconto
                .Where(c => c.PedidoId == orderId)
                .ToList();
        }

        public PedidoItem? LoadOrderItemForExchange(int orderItemId, int orderId, int customerId) {
            return _context.PedidoItens
                .Include(i => i.Pedido)
                .Include(i => i.Livro)
                .FirstOrDefault(i => i.Id == orderItemId && i.PedidoId == orderId && i.Pedido.ClienteId == customerId);
        }

        public Troca? LoadExchangeByOrderItemId(int orderItemId) {
            return _context.Trocas.FirstOrDefault(t => t.PedidoItemId == orderItemId);
        }

        public void AddOrderItem(PedidoItem orderItem) {
            _context.PedidoItens.Add(orderItem);
        }

        public void AddExchange(Troca exchange) {
            _context.Trocas.Add(exchange);
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
