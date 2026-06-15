using Livros.Domain;

namespace Livros.Application.CustomerWishlist {
    public interface ICustomerWishlistDataProvider {
        Task<Wishlist?> LoadWishlistAsync(int customerId, CancellationToken cancellationToken = default);
        Task<Livro?> LoadActiveBookAsync(int bookId, CancellationToken cancellationToken = default);
        Task<Cliente?> LoadActiveCustomerAsync(int customerId, CancellationToken cancellationToken = default);
        Task AddWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default);
        Task AddWishlistItemAsync(WishlistItem item, CancellationToken cancellationToken = default);
        void RemoveWishlistItem(WishlistItem item);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
