using Livros.Application.BookReviews;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class BookReviewDataProvider : IBookReviewDataProvider {
        private readonly AppDbContext _context;

        public BookReviewDataProvider(AppDbContext context) {
            _context = context;
        }

        public PedidoItem? LoadOrderItemForReview(int orderItemId, int orderId, int customerId) {
            return _context.PedidoItens
                .Include(item => item.Pedido)
                .Include(item => item.Livro)
                .FirstOrDefault(item =>
                    item.Id == orderItemId &&
                    item.PedidoId == orderId &&
                    item.Pedido.ClienteId == customerId);
        }

        public Avaliacao? LoadReview(int customerId, int orderId, int livroId) {
            return _context.Avaliacoes.FirstOrDefault(review =>
                review.ClienteId == customerId &&
                review.PedidoId == orderId &&
                review.LivroId == livroId);
        }

        public List<Avaliacao> LoadReviewsByBookId(int livroId) {
            return _context.Avaliacoes
                .Include(review => review.Cliente)
                .Where(review => review.LivroId == livroId)
                .ToList();
        }

        public void AddReview(Avaliacao review) {
            _context.Avaliacoes.Add(review);
        }

        public void SaveChanges() {
            _context.SaveChanges();
        }
    }
}
