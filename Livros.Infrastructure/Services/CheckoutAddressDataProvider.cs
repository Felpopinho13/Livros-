using Livros.Application.Checkout;
using Livros.Domain;
using Livros.Infrastructure.Data;
namespace Livros.Infrastructure.Services {
    public sealed class CheckoutAddressDataProvider : ICheckoutAddressDataProvider {
        private readonly AppDbContext _context;
        public CheckoutAddressDataProvider(AppDbContext context) {
            _context = context;
        }
        public Endereco? LoadDeliveryAddress(int clienteId, int enderecoId) {
            return _context.Enderecos
                .FirstOrDefault(e => e.Id == enderecoId && e.ClienteId == clienteId && e.IsEntrega);
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
        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
