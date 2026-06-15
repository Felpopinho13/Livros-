using Livros.Domain;

namespace Livros.Application.CustomerCheckout {
    public interface ICustomerCheckoutDataProvider {
        Livro? LoadActiveBookWithStock(int bookId);
        List<Endereco> LoadDeliveryAddressesByCustomerId(int customerId);
        List<Cartao> LoadCardsByCustomerIdWithBrand(int customerId);
        List<BandeiraCartao> LoadActiveBrands();
        List<CupomDesconto> LoadAvailableExchangeCouponsByCustomerId(int customerId);
        Estoque? LoadStockByBookId(int bookId);
        void SaveChanges();
    }
}
