using Livros.Application.AdminOrders;
using Livros.Application.Common.Logging;
using Livros.Domain;

namespace Livros.Application.BookReviews {
    public sealed class BookReviewService {
        private readonly IBookReviewDataProvider _dataProvider;
        private readonly IAppLogger<BookReviewService> _logger;

        public BookReviewService(
            IBookReviewDataProvider dataProvider,
            IAppLogger<BookReviewService> logger) {
            _dataProvider = dataProvider;
            _logger = logger;
        }

        public BookReviewSummaryResult GetSummary(int bookId) {
            if (bookId <= 0) {
                return new BookReviewSummaryResult();
            }

            var reviews = _dataProvider.LoadReviewsByBookId(bookId);
            if (reviews.Count == 0) {
                return new BookReviewSummaryResult();
            }

            return new BookReviewSummaryResult {
                AverageRating = Math.Round((decimal)reviews.Average(review => review.Nota), 1, MidpointRounding.AwayFromZero),
                ReviewCount = reviews.Count,
                Comments = reviews
                    .OrderByDescending(review => review.DataAvaliacao)
                    .Select(review => new BookReviewCommentResult {
                        CustomerName = ResolveCustomerName(review.Cliente?.Nome),
                        Rating = review.Nota,
                        Comment = review.Comentario,
                        ReviewDate = review.DataAvaliacao
                    })
                    .ToList()
            };
        }

        public BookReviewCreateResult Create(BookReviewCreateCommand command) {
            var orderItem = _dataProvider.LoadOrderItemForReview(command.OrderItemId, command.OrderId, command.CustomerId);
            if (orderItem == null) {
                return Failure("Nao foi possivel localizar o item para avaliacao.", found: false);
            }

            var displayStatus = OrderStatusHelper.NormalizeDisplayStatus(orderItem.Pedido?.Status, "Nao informado");
            if (displayStatus != "ENTREGUE") {
                return Failure("A avaliacao so pode ser enviada para livros de pedidos ENTREGUE.");
            }

            if (command.Rating < 1 || command.Rating > 5) {
                return Failure("Informe uma nota valida entre 1 e 5.");
            }

            var existingReview = _dataProvider.LoadReview(command.CustomerId, command.OrderId, orderItem.LivroId);
            if (existingReview != null) {
                return Failure("Voce ja avaliou este livro neste pedido.");
            }

            var normalizedComment = string.IsNullOrWhiteSpace(command.Comment)
                ? null
                : command.Comment.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedComment) && normalizedComment.Length > 1000) {
                return Failure("O comentario da avaliacao pode ter no maximo 1000 caracteres.");
            }

            var review = new Avaliacao {
                ClienteId = command.CustomerId,
                PedidoId = command.OrderId,
                LivroId = orderItem.LivroId,
                Nota = command.Rating,
                Comentario = normalizedComment,
                DataAvaliacao = DateTime.Now
            };

            _dataProvider.AddReview(review);
            _dataProvider.SaveChanges();

            _logger.LogInformation(
                "Avaliacao criada. ClienteId: {ClienteId}, PedidoId: {PedidoId}, LivroId: {LivroId}, Nota: {Nota}",
                command.CustomerId,
                command.OrderId,
                orderItem.LivroId,
                command.Rating);

            return new BookReviewCreateResult {
                OrderItemFound = true,
                Success = true,
                SuccessMessage = "Avaliacao enviada com sucesso."
            };
        }

        private static BookReviewCreateResult Failure(string message, bool found = true) {
            return new BookReviewCreateResult {
                OrderItemFound = found,
                Success = false,
                ErrorMessage = message
            };
        }

        private static string ResolveCustomerName(string? fullName) {
            if (string.IsNullOrWhiteSpace(fullName)) {
                return "Cliente";
            }

            return fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Cliente";
        }
    }
}
