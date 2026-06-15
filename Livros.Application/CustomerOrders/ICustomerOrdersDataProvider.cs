using Livros.Domain;

namespace Livros.Application.CustomerOrders {
    public interface ICustomerOrdersDataProvider {
        Pedido? LoadOrderWithItemsAndBooks(int orderId, int customerId);
        List<Pedido> LoadOrdersByCustomerIdWithItemsAndBooks(int customerId);
        List<Troca> LoadExchangesByOrderIds(IEnumerable<int> orderIds);
        Pedido? LoadDetailedOrder(int orderId, int customerId);
        List<Troca> LoadExchangesByOrderIdWithCoupon(int orderId);
        List<CupomDesconto> LoadCouponsByOrderId(int orderId);
        PedidoItem? LoadOrderItemForExchange(int orderItemId, int orderId, int customerId);
        Troca? LoadExchangeByOrderItemId(int orderItemId);
        void AddOrderItem(PedidoItem orderItem);
        void AddExchange(Troca exchange);
        void SaveChanges();
    }
}
