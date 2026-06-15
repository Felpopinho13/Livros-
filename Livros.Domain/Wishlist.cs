namespace Livros.Domain {
    public class Wishlist {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
        public bool IsAtiva { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public List<WishlistItem> Itens { get; set; } = new();
    }
}
