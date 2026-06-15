using Livros.Application.CustomerCards;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class CustomerCardDataProvider : ICustomerCardDataProvider {
        private readonly AppDbContext _context;

        public CustomerCardDataProvider(AppDbContext context) {
            _context = context;
        }

        public Cliente? LoadCustomerByEmailWithCards(string email) {
            return _context.Clientes
                .Include(c => c.Cartoes)
                    .ThenInclude(c => c.BandeiraCartao)
                .FirstOrDefault(c => c.Email == email);
        }

        public Cliente? LoadCustomerById(int clienteId) {
            return _context.Clientes.FirstOrDefault(c => c.Id == clienteId);
        }

        public List<Cartao> LoadCardsByCustomerId(int clienteId) {
            return _context.Cartoes
                .Where(c => c.ClienteId == clienteId)
                .ToList();
        }

        public BandeiraCartao? LoadActiveBrandById(int brandId) {
            return _context.BandeirasCartao.FirstOrDefault(b => b.Id == brandId && b.IsAtiva);
        }

        public List<BandeiraCartao> LoadActiveBrands() {
            return _context.BandeirasCartao
                .Where(b => b.IsAtiva)
                .OrderBy(b => b.Nome)
                .ToList();
        }

        public Cartao? LoadCardByIdForCustomer(string email, int cardId) {
            return _context.Cartoes
                .Include(c => c.Cliente)
                .FirstOrDefault(c => c.Id == cardId && c.Cliente.Email == email);
        }

        public void AddCard(Cartao card) {
            _context.Cartoes.Add(card);
        }

        public void RemoveCard(Cartao card) {
            _context.Cartoes.Remove(card);
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}