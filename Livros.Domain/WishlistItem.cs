namespace Livros.Domain {
    public class WishlistItem {
        public int Id { get; set; }
        public int WishlistId { get; set; }
        public Wishlist? Wishlist { get; set; }
        public int LivroId { get; set; }
        public Livro? Livro { get; set; }
        public DateTime DataAdicao { get; set; } = DateTime.Now;
    }
}
