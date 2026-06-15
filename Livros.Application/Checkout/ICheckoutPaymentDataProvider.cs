using Livros.Domain;
namespace Livros.Application.Checkout {
    public interface ICheckoutPaymentDataProvider {
        Cartao? LoadCustomerCardWithBrand(int clienteId, int cartaoId);
        bool IsCardBrandActive(int bandeiraCartaoId);
        void AddCard(Cartao cartao);
    }
}
