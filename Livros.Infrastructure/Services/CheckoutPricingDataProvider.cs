using Livros.Application.Checkout;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Livros.Infrastructure.Services {
    public sealed class CheckoutPricingDataProvider : ICheckoutPricingDataProvider {
        private readonly AppDbContext _context;
        public CheckoutPricingDataProvider(AppDbContext context) {
            _context = context;
        }
        public string? LoadStateForAddress(int clienteId, int enderecoId) {
            return _context.Enderecos
                .Where(e => e.Id == enderecoId && e.ClienteId == clienteId)
                .Select(e => e.Cidade.Estado.Sigla)
                .FirstOrDefault();
        }
        public CupomDesconto? LoadValidCoupon(int clienteId, string codigo) {
            return _context.CuponsDesconto
                .FirstOrDefault(c =>
                    c.IsAtivo &&
                    c.DataUtilizacao == null &&
                    c.Codigo.ToUpper() == codigo.ToUpper() &&
                    (!c.ClienteId.HasValue || c.ClienteId.Value == clienteId));
        }
        public List<CupomDesconto> LoadValidTradeCoupons(int clienteId, IReadOnlyCollection<int> ids) {
            if (ids.Count == 0) {
                return new List<CupomDesconto>();
            }
            return _context.CuponsDesconto
                .Where(c => ids.Contains(c.Id)
                    && c.ClienteId == clienteId
                    && c.IsAtivo
                    && c.DataUtilizacao == null
                    && c.Tipo == "TROCA")
                .OrderBy(c => c.Valor)
                .ToList();
        }
        public void AddCoupon(CupomDesconto cupom) {
            _context.CuponsDesconto.Add(cupom);
        }
    }
}
