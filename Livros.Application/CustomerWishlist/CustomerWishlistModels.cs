namespace Livros.Application.CustomerWishlist {
    public sealed class CustomerWishlistResult {
        public bool IsAuthenticated { get; init; }
        public int Count { get; init; }
        public List<CustomerWishlistItemResult> Items { get; init; } = new();
    }

    public sealed class CustomerWishlistItemResult {
        public int LivroId { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public decimal Preco { get; init; }
        public string ImagemUrl { get; init; } = string.Empty;
        public DateTime DataAdicao { get; init; }
    }

    public sealed class CustomerWishlistOperationResult {
        public bool Succeeded { get; init; }
        public bool RequiresAuthentication { get; init; }
        public string Message { get; init; } = string.Empty;
        public int Count { get; init; }
        public bool IsInWishlist { get; init; }
    }
}
