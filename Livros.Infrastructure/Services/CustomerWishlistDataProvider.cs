using Livros.Application.CustomerWishlist;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class CustomerWishlistDataProvider : ICustomerWishlistDataProvider {
        private readonly AppDbContext _context;

        public CustomerWishlistDataProvider(AppDbContext context) {
            _context = context;
        }

        public Task<Wishlist?> LoadWishlistAsync(int customerId, CancellationToken cancellationToken = default) {
            return _context.Wishlists
                .Include(wishlist => wishlist.Itens)
                    .ThenInclude(item => item.Livro)
                .FirstOrDefaultAsync(
                    wishlist => wishlist.ClienteId == customerId && wishlist.IsAtiva,
                    cancellationToken);
        }

        public Task<Livro?> LoadActiveBookAsync(int bookId, CancellationToken cancellationToken = default) {
            return _context.Livros
                .FirstOrDefaultAsync(book => book.Id == bookId && book.IsAtivo, cancellationToken);
        }

        public Task<Cliente?> LoadActiveCustomerAsync(int customerId, CancellationToken cancellationToken = default) {
            return _context.Clientes
                .FirstOrDefaultAsync(customer => customer.Id == customerId && customer.IsAtivo && !customer.IsAdmin, cancellationToken);
        }

        public Task AddWishlistAsync(Wishlist wishlist, CancellationToken cancellationToken = default) {
            return _context.Wishlists.AddAsync(wishlist, cancellationToken).AsTask();
        }

        public Task AddWishlistItemAsync(WishlistItem item, CancellationToken cancellationToken = default) {
            return _context.WishlistItems.AddAsync(item, cancellationToken).AsTask();
        }

        public void RemoveWishlistItem(WishlistItem item) {
            _context.WishlistItems.Remove(item);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
