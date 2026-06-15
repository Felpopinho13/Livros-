using Livros.Domain;

namespace Livros.Application.BookReviews {
    public interface IBookReviewDataProvider {
        PedidoItem? LoadOrderItemForReview(int orderItemId, int orderId, int customerId);
        Avaliacao? LoadReview(int customerId, int orderId, int livroId);
        List<Avaliacao> LoadReviewsByBookId(int livroId);
        void AddReview(Avaliacao review);
        void SaveChanges();
    }
}
