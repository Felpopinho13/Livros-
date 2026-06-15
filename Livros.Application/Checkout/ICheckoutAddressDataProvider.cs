using Livros.Domain;
namespace Livros.Application.Checkout {
    public interface ICheckoutAddressDataProvider {
        Endereco? LoadDeliveryAddress(int clienteId, int enderecoId);
        Estado? LoadStateByCode(string sigla);
        void AddState(Estado estado);
        Cidade? LoadCityByNameAndState(string nome, int estadoId);
        void AddCity(Cidade cidade);
        Bairro? LoadNeighborhoodByNameAndCity(string nome, int cidadeId);
        void AddNeighborhood(Bairro bairro);
        void AddAddress(Endereco endereco);
        void SaveChanges();
    }
}
