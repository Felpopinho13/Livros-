using Livros.Domain;
namespace Livros.Application.Checkout {
    public interface ICheckoutPricingDataProvider {
        string? LoadStateForAddress(int clienteId, int enderecoId);
        CupomDesconto? LoadValidCoupon(int clienteId, string codigo);
        List<CupomDesconto> LoadValidTradeCoupons(int clienteId, IReadOnlyCollection<int> ids);
        void AddCoupon(CupomDesconto cupom);
    }
}
