using Livros.Domain;
using System.Text.Json;

namespace Livros.Application.CustomerCart {
    public sealed class CustomerCartService {
        private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ReservationWarning = TimeSpan.FromMinutes(5);

        private readonly ICustomerCartDataProvider _dataProvider;

        public CustomerCartService(ICustomerCartDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public List<CustomerCartItemEntry> LoadStoredCart(int customerId) {
            var customer = _dataProvider.LoadCustomerById(customerId);
            if (string.IsNullOrWhiteSpace(customer?.CarrinhoPersistidoJson)) {
                return new List<CustomerCartItemEntry>();
            }

            return JsonSerializer.Deserialize<List<CustomerCartItemEntry>>(customer.CarrinhoPersistidoJson)
                ?? new List<CustomerCartItemEntry>();
        }

        public CustomerCartActionResult AddItem(CustomerCartAddCommand command) {
            var now = DateTime.Now;
            ClearExpiredReservations(now);

            var book = _dataProvider.LoadActiveBookWithStock(command.LivroId);
            if (book == null) {
                return Failure(command.Items, "Nao foi possivel adicionar o livro ao carrinho.");
            }

            var stockAvailable = book.Estoque?.Quantidade ?? 0;
            if (stockAvailable <= 0) {
                return Failure(command.Items, $"O livro \"{book.Titulo}\" esta sem estoque no momento.");
            }

            var quantity = Math.Max(1, command.Quantidade);
            var items = CloneItems(command.Items);
            var existingItem = items.FirstOrDefault(i => i.LivroId == command.LivroId);
            var desiredQuantity = (existingItem?.Quantidade ?? 0) + quantity;
            var availableToUser = GetAvailableQuantityForUser(command.LivroId, stockAvailable, command.CustomerId, command.SessionKey, now);
            var finalQuantity = Math.Min(desiredQuantity, availableToUser);

            if (finalQuantity <= 0) {
                return Failure(items, $"O livro \"{book.Titulo}\" nao possui saldo disponivel para reserva no momento.");
            }

            if (existingItem == null) {
                items.Add(new CustomerCartItemEntry {
                    LivroId = command.LivroId,
                    Quantidade = finalQuantity
                });
            }
            else {
                existingItem.Quantidade = finalQuantity;
            }

            UpsertReservation(command.LivroId, finalQuantity, command.CustomerId, command.SessionKey, now, renewExpiration: true);
            PersistCustomerCart(command.CustomerId, items);
            _dataProvider.SaveChanges();

            return new CustomerCartActionResult {
                Success = true,
                Items = items,
                SuccessMessage = finalQuantity < desiredQuantity ? null : $"\"{book.Titulo}\" foi adicionado ao carrinho.",
                WarningMessage = finalQuantity < desiredQuantity
                    ? $"O estoque reservado de \"{book.Titulo}\" foi ajustado para {finalQuantity} unidade(s)."
                    : null
            };
        }

        public CustomerCartActionResult UpdateItem(CustomerCartUpdateCommand command) {
            var now = DateTime.Now;
            ClearExpiredReservations(now);

            var items = CloneItems(command.Items);
            var item = items.FirstOrDefault(i => i.LivroId == command.LivroId);
            if (item == null) {
                return new CustomerCartActionResult {
                    Success = false,
                    ItemFound = false,
                    Items = items
                };
            }

            if (command.Quantidade <= 0) {
                items.Remove(item);
                RemoveReservationsForBook(command.LivroId, command.CustomerId, command.SessionKey);
                PersistCustomerCart(command.CustomerId, items);
                _dataProvider.SaveChanges();

                return new CustomerCartActionResult {
                    Success = true,
                    Items = items
                };
            }

            var book = _dataProvider.LoadActiveBookWithStock(command.LivroId);
            if (book == null) {
                items.Remove(item);
                RemoveReservationsForBook(command.LivroId, command.CustomerId, command.SessionKey);
                PersistCustomerCart(command.CustomerId, items);
                _dataProvider.SaveChanges();

                return Failure(items, "O item nao esta mais disponivel.");
            }

            var stockAvailable = book.Estoque?.Quantidade ?? 0;
            var availableToUser = GetAvailableQuantityForUser(command.LivroId, stockAvailable, command.CustomerId, command.SessionKey, now);
            var finalQuantity = Math.Min(Math.Max(1, command.Quantidade), availableToUser);

            if (finalQuantity <= 0) {
                items.Remove(item);
                RemoveReservationsForBook(command.LivroId, command.CustomerId, command.SessionKey);
                PersistCustomerCart(command.CustomerId, items);
                _dataProvider.SaveChanges();

                return Failure(items, $"O livro \"{book.Titulo}\" ficou sem saldo reservado no momento.");
            }

            item.Quantidade = finalQuantity;
            UpsertReservation(command.LivroId, finalQuantity, command.CustomerId, command.SessionKey, now, renewExpiration: true);
            PersistCustomerCart(command.CustomerId, items);
            _dataProvider.SaveChanges();

            return new CustomerCartActionResult {
                Success = true,
                Items = items,
                WarningMessage = finalQuantity < command.Quantidade
                    ? $"A quantidade de \"{book.Titulo}\" foi ajustada para {finalQuantity} unidade(s) por falta de estoque reservado."
                    : null
            };
        }

        public CustomerCartActionResult RemoveItem(CustomerCartRemoveCommand command) {
            var items = CloneItems(command.Items);
            items.RemoveAll(i => i.LivroId == command.LivroId);
            RemoveReservationsForBook(command.LivroId, command.CustomerId, command.SessionKey);
            PersistCustomerCart(command.CustomerId, items);
            _dataProvider.SaveChanges();

            return new CustomerCartActionResult {
                Success = true,
                Items = items
            };
        }

        public void Clear(CustomerCartClearCommand command) {
            PersistCustomerCart(command.CustomerId, new List<CustomerCartItemEntry>());
            var reservations = _dataProvider.LoadReservationsByUser(command.CustomerId, command.SessionKey);
            if (reservations.Any()) {
                _dataProvider.RemoveReservations(reservations);
            }

            _dataProvider.SaveChanges();
        }

        public CustomerCartSyncResult Synchronize(CustomerCartSyncCommand command) {
            var result = new CustomerCartSyncResult();
            var now = DateTime.Now;
            var expiredBookIds = _dataProvider.LoadExpiredReservations(now)
                .Where(r => ReservationBelongsToUser(r, command.CustomerId, command.SessionKey))
                .Select(r => r.LivroId)
                .ToHashSet();

            ClearExpiredReservations(now);

            var items = CloneItems(command.Items);
            if (!items.Any()) {
                return result;
            }

            var bookIds = items.Select(i => i.LivroId).Distinct().ToList();
            var books = _dataProvider.LoadActiveBooksWithStock(bookIds);
            var activeReservations = _dataProvider.LoadReservationsByBookIds(bookIds)
                .Where(r => r.ExpiraEm > now)
                .ToList();

            foreach (var item in items) {
                if (expiredBookIds.Contains(item.LivroId)) {
                    result.CarrinhoMudou = true;
                    result.RequerRevisao = true;
                    result.Avisos.Add("Um item foi removido do carrinho porque a reserva expirou.");
                    continue;
                }

                var book = books.FirstOrDefault(l => l.Id == item.LivroId);
                if (book == null) {
                    RemoveReservationsForBook(item.LivroId, command.CustomerId, command.SessionKey);
                    result.CarrinhoMudou = true;
                    result.RequerRevisao = true;
                    result.Avisos.Add("Um item foi removido do carrinho porque nao esta mais disponivel.");
                    continue;
                }

                var stockAvailable = book.Estoque?.Quantidade ?? 0;
                var quantityReservedByOthers = activeReservations
                    .Where(r => r.LivroId == item.LivroId && !ReservationBelongsToUser(r, command.CustomerId, command.SessionKey))
                    .Sum(r => r.Quantidade);

                var availableToUser = Math.Max(stockAvailable - quantityReservedByOthers, 0);
                var adjustedQuantity = Math.Min(item.Quantidade, availableToUser);

                if (adjustedQuantity <= 0) {
                    RemoveReservationsForBook(item.LivroId, command.CustomerId, command.SessionKey);
                    result.CarrinhoMudou = true;
                    result.RequerRevisao = true;
                    result.Avisos.Add($"\"{book.Titulo}\" foi removido do carrinho porque ficou sem estoque.");
                    continue;
                }

                if (adjustedQuantity != item.Quantidade) {
                    result.CarrinhoMudou = true;
                    result.RequerRevisao = true;
                    result.Avisos.Add($"A quantidade de \"{book.Titulo}\" foi ajustada para {adjustedQuantity} unidade(s) por alteracao de estoque.");
                }

                var reservation = activeReservations
                    .FirstOrDefault(r => r.LivroId == item.LivroId && ReservationBelongsToUser(r, command.CustomerId, command.SessionKey));

                if (reservation == null) {
                    reservation = UpsertReservation(item.LivroId, adjustedQuantity, command.CustomerId, command.SessionKey, now, renewExpiration: true);
                    activeReservations.Add(reservation);
                }
                else if (reservation.Quantidade != adjustedQuantity || command.RenewReservations) {
                    reservation = UpsertReservation(item.LivroId, adjustedQuantity, command.CustomerId, command.SessionKey, now, renewExpiration: true);
                }

                var remainingTime = reservation.ExpiraEm - now;
                var reservationExpiring = remainingTime <= ReservationWarning;

                result.Itens.Add(new CustomerCartNormalizedItem {
                    Livro = book,
                    Quantidade = adjustedQuantity,
                    EstoqueDisponivel = availableToUser,
                    ReservaExpiraEm = reservation.ExpiraEm,
                    ReservaExpirando = reservationExpiring,
                    AvisoReserva = reservationExpiring
                        ? $"Reserva expira em {Math.Max((int)Math.Ceiling(remainingTime.TotalMinutes), 0)} minuto(s)."
                        : null
                });
            }

            var updatedItems = result.Itens
                .Select(i => new CustomerCartItemEntry { LivroId = i.Livro.Id, Quantidade = i.Quantidade })
                .ToList();

            if (CartChanged(items, updatedItems)) {
                result.CarrinhoMudou = true;
                PersistCustomerCart(command.CustomerId, updatedItems);
            }

            result.UpdatedItems = updatedItems;
            _dataProvider.SaveChanges();
            return result;
        }

        private void PersistCustomerCart(int? customerId, IReadOnlyCollection<CustomerCartItemEntry> items) {
            if (!customerId.HasValue) {
                return;
            }

            var customer = _dataProvider.LoadCustomerById(customerId.Value);
            if (customer == null) {
                return;
            }

            customer.CarrinhoPersistidoJson = items.Any()
                ? JsonSerializer.Serialize(items)
                : null;
        }

        private int GetAvailableQuantityForUser(int bookId, int stockAvailable, int? customerId, string sessionKey, DateTime now) {
            var quantityReservedByOthers = _dataProvider.LoadReservationsByBookIds(new[] { bookId })
                .Where(r => r.ExpiraEm > now)
                .Where(r => !ReservationBelongsToUser(r, customerId, sessionKey))
                .Sum(r => r.Quantidade);

            return Math.Max(stockAvailable - quantityReservedByOthers, 0);
        }

        private void ClearExpiredReservations(DateTime now) {
            var expiredReservations = _dataProvider.LoadExpiredReservations(now);
            if (expiredReservations.Any()) {
                _dataProvider.RemoveReservations(expiredReservations);
                _dataProvider.SaveChanges();
            }
        }

        private ReservaCarrinho UpsertReservation(int bookId, int quantity, int? customerId, string sessionKey, DateTime now, bool renewExpiration) {
            var reservation = _dataProvider.LoadReservationsByBookIds(new[] { bookId })
                .FirstOrDefault(r => r.ExpiraEm > now && ReservationBelongsToUser(r, customerId, sessionKey));

            if (reservation == null) {
                reservation = new ReservaCarrinho {
                    LivroId = bookId,
                    ClienteId = customerId,
                    SessionKey = customerId.HasValue ? null : sessionKey,
                    Quantidade = quantity,
                    ReservadoEm = now,
                    ExpiraEm = now.Add(ReservationDuration)
                };

                _dataProvider.AddReservation(reservation);
                return reservation;
            }

            reservation.Quantidade = quantity;
            reservation.ReservadoEm = now;
            if (renewExpiration) {
                reservation.ExpiraEm = now.Add(ReservationDuration);
            }

            return reservation;
        }

        private void RemoveReservationsForBook(int bookId, int? customerId, string sessionKey) {
            var reservations = _dataProvider.LoadReservationsByBookIds(new[] { bookId })
                .Where(r => ReservationBelongsToUser(r, customerId, sessionKey))
                .ToList();

            if (reservations.Any()) {
                _dataProvider.RemoveReservations(reservations);
            }
        }

        private static bool ReservationBelongsToUser(ReservaCarrinho reservation, int? customerId, string sessionKey) {
            if (customerId.HasValue) {
                return reservation.ClienteId == customerId.Value;
            }

            return !reservation.ClienteId.HasValue
                   && !string.IsNullOrWhiteSpace(reservation.SessionKey)
                   && string.Equals(reservation.SessionKey, sessionKey, StringComparison.Ordinal);
        }

        private static List<CustomerCartItemEntry> CloneItems(IEnumerable<CustomerCartItemEntry> items) {
            return items
                .Select(i => new CustomerCartItemEntry {
                    LivroId = i.LivroId,
                    Quantidade = i.Quantidade
                })
                .ToList();
        }

        private static bool CartChanged(List<CustomerCartItemEntry> original, List<CustomerCartItemEntry> updated) {
            if (original.Count != updated.Count) {
                return true;
            }

            for (var index = 0; index < updated.Count; index++) {
                if (original[index].LivroId != updated[index].LivroId
                    || original[index].Quantidade != updated[index].Quantidade) {
                    return true;
                }
            }

            return false;
        }

        private static CustomerCartActionResult Failure(List<CustomerCartItemEntry> items, string message) {
            return new CustomerCartActionResult {
                Success = false,
                Items = items,
                ErrorMessage = message
            };
        }
    }
}
