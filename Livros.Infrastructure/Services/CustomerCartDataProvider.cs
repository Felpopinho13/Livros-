using Livros.Application.CustomerCart;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class CustomerCartDataProvider : ICustomerCartDataProvider {
        private readonly AppDbContext _context;

        public CustomerCartDataProvider(AppDbContext context) {
            _context = context;
        }

        public Livro? LoadActiveBookWithStock(int bookId) {
            return _context.Livros
                .Include(l => l.Estoque)
                .FirstOrDefault(l => l.Id == bookId && l.IsAtivo);
        }

        public List<Livro> LoadActiveBooksWithStock(IEnumerable<int> bookIds) {
            var ids = bookIds.Distinct().ToList();
            if (!ids.Any()) {
                return new List<Livro>();
            }

            return _context.Livros
                .Include(l => l.Estoque)
                .Where(l => ids.Contains(l.Id) && l.IsAtivo)
                .ToList();
        }

        public List<ReservaCarrinho> LoadReservationsByBookIds(IEnumerable<int> bookIds) {
            var ids = bookIds.Distinct().ToList();
            if (!ids.Any()) {
                return new List<ReservaCarrinho>();
            }

            return _context.ReservasCarrinho
                .Where(r => ids.Contains(r.LivroId))
                .ToList();
        }

        public List<ReservaCarrinho> LoadReservationsByUser(int? customerId, string sessionKey) {
            if (customerId.HasValue) {
                return _context.ReservasCarrinho
                    .Where(r => r.ClienteId == customerId.Value)
                    .ToList();
            }

            return _context.ReservasCarrinho
                .Where(r => !r.ClienteId.HasValue && r.SessionKey == sessionKey)
                .ToList();
        }

        public List<ReservaCarrinho> LoadExpiredReservations(DateTime now) {
            return _context.ReservasCarrinho
                .Where(r => r.ExpiraEm <= now)
                .ToList();
        }

        public Cliente? LoadCustomerById(int customerId) {
            return _context.Clientes.FirstOrDefault(c => c.Id == customerId);
        }

        public void AddReservation(ReservaCarrinho reservation) {
            _context.ReservasCarrinho.Add(reservation);
        }

        public void RemoveReservations(IEnumerable<ReservaCarrinho> reservations) {
            _context.ReservasCarrinho.RemoveRange(reservations);
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
