using Livros.Application.CustomerCheckout;
using Livros.Domain;
using Livros.Infrastructure.Data;

namespace Livros.Infrastructure.Services {
    public sealed class CustomerOrderPlacementDataProvider : ICustomerOrderPlacementDataProvider {
        private readonly AppDbContext _context;

        public CustomerOrderPlacementDataProvider(AppDbContext context) {
            _context = context;
        }

        public void AddOrder(Pedido order) {
            _context.Pedidos.Add(order);
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
