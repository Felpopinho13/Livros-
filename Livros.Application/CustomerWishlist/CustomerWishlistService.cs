using Livros.Application.Common.Logging;
using Livros.Domain;

namespace Livros.Application.CustomerWishlist {
    public sealed class CustomerWishlistService {
        private readonly ICustomerWishlistDataProvider _dataProvider;
        private readonly IAppLogger<CustomerWishlistService> _logger;

        public CustomerWishlistService(
            ICustomerWishlistDataProvider dataProvider,
            IAppLogger<CustomerWishlistService> logger) {
            _dataProvider = dataProvider;
            _logger = logger;
        }

        public async Task<CustomerWishlistResult> BuildAsync(int? customerId, CancellationToken cancellationToken = default) {
            if (!customerId.HasValue || customerId.Value <= 0) {
                return new CustomerWishlistResult {
                    IsAuthenticated = false
                };
            }

            var wishlist = await _dataProvider.LoadWishlistAsync(customerId.Value, cancellationToken);
            if (wishlist == null) {
                return new CustomerWishlistResult {
                    IsAuthenticated = true
                };
            }

            var items = wishlist.Itens
                .Where(item => item.Livro != null)
                .OrderByDescending(item => item.DataAdicao)
                .Select(item => new CustomerWishlistItemResult {
                    LivroId = item.LivroId,
                    Titulo = item.Livro!.Titulo,
                    Preco = item.Livro.Preco,
                    ImagemUrl = item.Livro.ImagemUrl ?? string.Empty,
                    DataAdicao = item.DataAdicao
                })
                .ToList();

            return new CustomerWishlistResult {
                IsAuthenticated = true,
                Count = items.Count,
                Items = items
            };
        }

        public async Task<CustomerWishlistOperationResult> AddAsync(int? customerId, int bookId, CancellationToken cancellationToken = default) {
            if (!customerId.HasValue || customerId.Value <= 0) {
                return Failure("Faça login para salvar livros na sua lista de desejos.", requiresAuthentication: true);
            }

            if (bookId <= 0) {
                return Failure("Livro inválido para a lista de desejos.");
            }

            var customer = await _dataProvider.LoadActiveCustomerAsync(customerId.Value, cancellationToken);
            if (customer == null) {
                _logger.LogWarning("Cliente nao encontrado ao adicionar item na wishlist. ClienteId: {ClienteId}", customerId.Value);
                return Failure("Cliente não encontrado.");
            }

            var book = await _dataProvider.LoadActiveBookAsync(bookId, cancellationToken);
            if (book == null) {
                _logger.LogWarning("Livro nao encontrado ao adicionar item na wishlist. ClienteId: {ClienteId}, LivroId: {LivroId}", customerId.Value, bookId);
                return Failure("Livro não encontrado.");
            }

            var wishlist = await EnsureWishlistAsync(customer.Id, cancellationToken);
            var existingItem = wishlist.Itens.FirstOrDefault(item => item.LivroId == bookId);
            if (existingItem != null) {
                return new CustomerWishlistOperationResult {
                    Succeeded = true,
                    Message = "O livro já está na sua lista de desejos.",
                    Count = wishlist.Itens.Count,
                    IsInWishlist = true
                };
            }

            var item = new WishlistItem {
                WishlistId = wishlist.Id,
                LivroId = book.Id,
                DataAdicao = DateTime.Now
            };

            await _dataProvider.AddWishlistItemAsync(item, cancellationToken);
            wishlist.Itens.Add(item);
            await _dataProvider.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Livro adicionado na wishlist. ClienteId: {ClienteId}, WishlistId: {WishlistId}, LivroId: {LivroId}", customer.Id, wishlist.Id, book.Id);

            return new CustomerWishlistOperationResult {
                Succeeded = true,
                Message = "Livro adicionado à sua lista de desejos.",
                Count = wishlist.Itens.Count,
                IsInWishlist = true
            };
        }

        public async Task<CustomerWishlistOperationResult> RemoveAsync(int? customerId, int bookId, CancellationToken cancellationToken = default) {
            if (!customerId.HasValue || customerId.Value <= 0) {
                return Failure("Faça login para gerenciar sua lista de desejos.", requiresAuthentication: true);
            }

            if (bookId <= 0) {
                return Failure("Livro inválido para remoção.");
            }

            var wishlist = await _dataProvider.LoadWishlistAsync(customerId.Value, cancellationToken);
            if (wishlist == null) {
                return new CustomerWishlistOperationResult {
                    Succeeded = true,
                    Message = "A lista de desejos já estava vazia.",
                    Count = 0,
                    IsInWishlist = false
                };
            }

            var item = wishlist.Itens.FirstOrDefault(entry => entry.LivroId == bookId);
            if (item == null) {
                return new CustomerWishlistOperationResult {
                    Succeeded = true,
                    Message = "O livro já não estava na sua lista de desejos.",
                    Count = wishlist.Itens.Count,
                    IsInWishlist = false
                };
            }

            _dataProvider.RemoveWishlistItem(item);
            wishlist.Itens.Remove(item);
            await _dataProvider.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Livro removido da wishlist. ClienteId: {ClienteId}, WishlistId: {WishlistId}, LivroId: {LivroId}", customerId.Value, wishlist.Id, bookId);

            return new CustomerWishlistOperationResult {
                Succeeded = true,
                Message = "Livro removido da sua lista de desejos.",
                Count = wishlist.Itens.Count,
                IsInWishlist = false
            };
        }

        private async Task<Wishlist> EnsureWishlistAsync(int customerId, CancellationToken cancellationToken) {
            var wishlist = await _dataProvider.LoadWishlistAsync(customerId, cancellationToken);
            if (wishlist != null) {
                return wishlist;
            }

            wishlist = new Wishlist {
                ClienteId = customerId,
                IsAtiva = true,
                DataCriacao = DateTime.Now
            };

            await _dataProvider.AddWishlistAsync(wishlist, cancellationToken);
            await _dataProvider.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Wishlist criada automaticamente para o cliente {ClienteId}.", customerId);

            return wishlist;
        }

        private static CustomerWishlistOperationResult Failure(string message, bool requiresAuthentication = false) {
            return new CustomerWishlistOperationResult {
                Succeeded = false,
                RequiresAuthentication = requiresAuthentication,
                Message = message
            };
        }
    }
}
