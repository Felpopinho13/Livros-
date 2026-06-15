using Livros.Application.CustomerAddresses;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class CustomerAddressDataProvider : ICustomerAddressDataProvider {
        private readonly AppDbContext _context;

        public CustomerAddressDataProvider(AppDbContext context) {
            _context = context;
        }

        public Cliente? LoadCustomerByEmailWithAddresses(string email) {
            return _context.Clientes
                .Include(c => c.Enderecos)
                    .ThenInclude(e => e.Cidade)
                        .ThenInclude(c => c.Estado)
                .Include(c => c.Enderecos)
                    .ThenInclude(e => e.Bairro)
                .FirstOrDefault(c => c.Email == email);
        }

        public Cliente? LoadCustomerById(int clienteId) {
            return _context.Clientes.FirstOrDefault(c => c.Id == clienteId);
        }

        public List<Endereco> LoadAddressesByCustomerId(int clienteId) {
            return _context.Enderecos
                .Where(e => e.ClienteId == clienteId)
                .ToList();
        }

        public Endereco? LoadSavedAddressByIdWithRelationsForCustomer(string email, int addressId) {
            return _context.Enderecos
                .Include(e => e.Cliente)
                .Include(e => e.Bairro)
                    .ThenInclude(b => b.Cidade)
                .Include(e => e.Cidade)
                    .ThenInclude(c => c.Estado)
                .FirstOrDefault(e => e.Id == addressId && e.Cliente.Email == email && (e.IsEntrega || e.IsCobranca));
        }

        public Estado? LoadStateByCode(string sigla) {
            return _context.Estados.FirstOrDefault(e => e.Sigla == sigla);
        }

        public void AddState(Estado estado) {
            _context.Estados.Add(estado);
        }

        public Cidade? LoadCityByNameAndState(string nome, int estadoId) {
            return _context.Cidades.FirstOrDefault(c => c.Nome == nome && c.EstadoId == estadoId);
        }

        public void AddCity(Cidade cidade) {
            _context.Cidades.Add(cidade);
        }

        public Bairro? LoadNeighborhoodByNameAndCity(string nome, int cidadeId) {
            return _context.Bairros.FirstOrDefault(b => b.Nome == nome && b.CidadeId == cidadeId);
        }

        public void AddNeighborhood(Bairro bairro) {
            _context.Bairros.Add(bairro);
        }

        public void AddAddress(Endereco endereco) {
            _context.Enderecos.Add(endereco);
        }

        public bool HasOrdersUsingAddress(int addressId) {
            return _context.Pedidos.Any(p => p.EnderecoId == addressId);
        }

        public void RemoveAddress(Endereco endereco) {
            _context.Enderecos.Remove(endereco);
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}