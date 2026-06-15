namespace Livros.Application.BookReviews {
    public sealed class BookReviewSummaryResult {
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<BookReviewCommentResult> Comments { get; set; } = new();
    }

    public sealed class BookReviewCommentResult {
        public string CustomerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }

    public sealed class BookReviewCreateCommand {
        public int CustomerId { get; set; }
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public sealed class BookReviewCreateResult {
        public bool OrderItemFound { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
