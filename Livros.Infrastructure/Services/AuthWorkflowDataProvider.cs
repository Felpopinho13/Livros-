using Livros.Application.Authentication;
using Livros.Domain;
using Livros.Infrastructure.Data;

namespace Livros.Infrastructure.Services {
    public sealed class AuthWorkflowDataProvider : IAuthWorkflowDataProvider {
        private readonly AppDbContext _context;

        public AuthWorkflowDataProvider(AppDbContext context) {
            _context = context;
        }

        public bool ActiveEmailExists(string email) {
            return _context.Clientes.Any(c => c.Email == email && c.IsAtivo);
        }

        public string HashPassword(string password) {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public Estado? LoadStateByCode(string stateCode) {
            return _context.Estados.FirstOrDefault(e => e.Sigla == stateCode);
        }

        public void AddState(Estado state) {
            _context.Estados.Add(state);
        }

        public Cidade? LoadCityByNameAndState(string cityName, int stateId) {
            return _context.Cidades.FirstOrDefault(c => c.Nome == cityName && c.EstadoId == stateId);
        }

        public void AddCity(Cidade city) {
            _context.Cidades.Add(city);
        }

        public Bairro? LoadNeighborhoodByNameAndCity(string neighborhoodName, int cityId) {
            return _context.Bairros.FirstOrDefault(b => b.Nome == neighborhoodName && b.CidadeId == cityId);
        }

        public void AddNeighborhood(Bairro neighborhood) {
            _context.Bairros.Add(neighborhood);
        }

        public void AddCustomer(Cliente customer) {
            _context.Clientes.Add(customer);
        }

        public List<ReservaCarrinho> LoadAnonymousReservations(string sessionKey) {
            return _context.ReservasCarrinho
                .Where(r => r.SessionKey == sessionKey && r.ClienteId == null)
                .ToList();
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
