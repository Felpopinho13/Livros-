using Livros.Domain;

namespace Livros.Application.CustomerAddresses {
    public interface ICustomerAddressDataProvider {
        Cliente? LoadCustomerByEmailWithAddresses(string email);
        Cliente? LoadCustomerById(int clienteId);
        List<Endereco> LoadAddressesByCustomerId(int clienteId);
        Endereco? LoadSavedAddressByIdWithRelationsForCustomer(string email, int addressId);
        Estado? LoadStateByCode(string sigla);
        void AddState(Estado estado);
        Cidade? LoadCityByNameAndState(string nome, int estadoId);
        void AddCity(Cidade cidade);
        Bairro? LoadNeighborhoodByNameAndCity(string nome, int cidadeId);
        void AddNeighborhood(Bairro bairro);
        void AddAddress(Endereco endereco);
        bool HasOrdersUsingAddress(int addressId);
        void RemoveAddress(Endereco endereco);
        void SaveChanges();
    }
}