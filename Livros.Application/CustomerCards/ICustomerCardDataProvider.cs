using Livros.Domain;

namespace Livros.Application.CustomerCards {
    public interface ICustomerCardDataProvider {
        Cliente? LoadCustomerByEmailWithCards(string email);
        Cliente? LoadCustomerById(int clienteId);
        List<Cartao> LoadCardsByCustomerId(int clienteId);
        BandeiraCartao? LoadActiveBrandById(int brandId);
        List<BandeiraCartao> LoadActiveBrands();
        Cartao? LoadCardByIdForCustomer(string email, int cardId);
        void AddCard(Cartao card);
        void RemoveCard(Cartao card);
        void SaveChanges();
    }
}