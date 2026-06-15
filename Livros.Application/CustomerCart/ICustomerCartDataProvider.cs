using Livros.Domain;

namespace Livros.Application.CustomerCart {
    public interface ICustomerCartDataProvider {
        Livro? LoadActiveBookWithStock(int bookId);
        List<Livro> LoadActiveBooksWithStock(IEnumerable<int> bookIds);
        List<ReservaCarrinho> LoadReservationsByBookIds(IEnumerable<int> bookIds);
        List<ReservaCarrinho> LoadReservationsByUser(int? customerId, string sessionKey);
        List<ReservaCarrinho> LoadExpiredReservations(DateTime now);
        Cliente? LoadCustomerById(int customerId);
        void AddReservation(ReservaCarrinho reservation);
        void RemoveReservations(IEnumerable<ReservaCarrinho> reservations);
        void SaveChanges();
    }
}
