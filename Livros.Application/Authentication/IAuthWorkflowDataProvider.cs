using Livros.Domain;

namespace Livros.Application.Authentication {
    public interface IAuthWorkflowDataProvider {
        bool ActiveEmailExists(string email);
        string HashPassword(string password);
        Estado? LoadStateByCode(string stateCode);
        void AddState(Estado state);
        Cidade? LoadCityByNameAndState(string cityName, int stateId);
        void AddCity(Cidade city);
        Bairro? LoadNeighborhoodByNameAndCity(string neighborhoodName, int cityId);
        void AddNeighborhood(Bairro neighborhood);
        void AddCustomer(Cliente customer);
        List<ReservaCarrinho> LoadAnonymousReservations(string sessionKey);
        void SaveChanges();
    }
}
