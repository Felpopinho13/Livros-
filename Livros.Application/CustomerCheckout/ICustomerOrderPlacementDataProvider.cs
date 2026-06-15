using Livros.Domain;

namespace Livros.Application.CustomerCheckout {
    public interface ICustomerOrderPlacementDataProvider {
        void AddOrder(Pedido order);
        void SaveChanges();
    }
}
