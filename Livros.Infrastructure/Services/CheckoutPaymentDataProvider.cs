using Livros.Application.Checkout;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Livros.Infrastructure.Services {
    public sealed class CheckoutPaymentDataProvider : ICheckoutPaymentDataProvider {
        private readonly AppDbContext _context;
        public CheckoutPaymentDataProvider(AppDbContext context) {
            _context = context;
        }
        public Cartao? LoadCustomerCardWithBrand(int clienteId, int cartaoId) {
            return _context.Cartoes
                .Include(c => c.BandeiraCartao)
                .FirstOrDefault(c => c.Id == cartaoId && c.ClienteId == clienteId);
        }
        public bool IsCardBrandActive(int bandeiraCartaoId) {
            return _context.BandeirasCartao.Any(b => b.Id == bandeiraCartaoId && b.IsAtiva);
        }
        public void AddCard(Cartao cartao) {
            _context.Cartoes.Add(cartao);
        }
    }
}
