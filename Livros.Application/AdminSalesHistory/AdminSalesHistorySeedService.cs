using Livros.Domain;

namespace Livros.Application.AdminSalesHistory {
    public sealed class AdminSalesHistorySeedService {
        private static readonly string[] EligibleOrderStatuses = {
            "APROVADA",
            "PAGAMENTO APROVADO",
            "EM SEPARACAO",
            "EM TRANSPORTE",
            "ENVIADO",
            "ENTREGUE"
        };

        private static readonly string[] DeliveryTypes = {
            "PADRAO",
            "PROGRAMADA"
        };

        private static readonly string[] PaymentMethods = {
            "cartao",
            "pix",
            "boleto"
        };

        private static readonly string[] DemoCustomerNames = {
            "Ana Martins",
            "Bruno Almeida",
            "Carla Souza",
            "Diego Lima",
            "Fernanda Rocha",
            "Gabriel Costa",
            "Helena Ribeiro",
            "Igor Mendes",
            "Julia Nogueira",
            "Lucas Araujo",
            "Marina Teixeira",
            "Paulo Silveira"
        };

        private readonly IAdminSalesHistorySeedDataProvider _dataProvider;

        public AdminSalesHistorySeedService(IAdminSalesHistorySeedDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public async Task<AdminSalesHistorySeedResult> GenerateAsync(int months = 13, CancellationToken cancellationToken = default) {
            if (months < 13) {
                months = 13;
            }

            var random = new Random();
            var referenceDate = DateTime.Today;
            var books = await _dataProvider.LoadEligibleBooksAsync(cancellationToken);

            if (books.Count < 3) {
                return AdminSalesHistorySeedResult.Fail("Cadastre pelo menos 3 livros ativos para gerar o historico de vendas.");
            }

            var geography = await _dataProvider.EnsureGeographyAsync(cancellationToken);
            var customers = await _dataProvider.EnsureCustomersAsync(geography, DemoCustomerNames, cancellationToken);

            if (!customers.Any()) {
                return AdminSalesHistorySeedResult.Fail("Nao foi possivel preparar clientes para o historico de vendas.");
            }

            EnsureStockRecords(books);

            var monthStarts = Enumerable.Range(0, months)
                .Select(offset => new DateTime(referenceDate.Year, referenceDate.Month, 1).AddMonths(-(months - 1) + offset))
                .ToList();

            var ordersCreated = 0;
            var itemsCreated = 0;
            var unitsSold = 0;
            var customersUsed = new HashSet<int>();

            foreach (var monthStart in monthStarts) {
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var ordersInMonth = random.Next(8, 23);

                for (var orderIndex = 0; orderIndex < ordersInMonth; orderIndex++) {
                    var customer = customers[random.Next(customers.Count)];
                    var address = customer.Enderecos!.First();
                    var orderDate = GenerateRandomDate(random, monthStart, monthEnd);
                    var selectedBooks = books
                        .OrderBy(_ => random.Next())
                        .Take(random.Next(1, Math.Min(4, books.Count) + 1))
                        .ToList();

                    var orderItems = new List<PedidoItem>();
                    decimal subtotal = 0;

                    foreach (var book in selectedBooks) {
                        var quantity = random.Next(1, 4);
                        EnsureStockAvailable(book, quantity, random);

                        var item = new PedidoItem {
                            LivroId = book.Id,
                            Livro = book,
                            Quantidade = quantity,
                            PrecoUnitario = book.Preco
                        };

                        orderItems.Add(item);
                        subtotal += item.PrecoUnitario * item.Quantidade;
                        book.Estoque.Quantidade -= quantity;
                        itemsCreated++;
                        unitsSold += quantity;
                    }

                    var shipping = decimal.Round((decimal)(random.NextDouble() * 16) + 8, 2);
                    var total = subtotal + shipping;
                    var deliveryType = DeliveryTypes[random.Next(DeliveryTypes.Length)];
                    var status = PickOrderStatus(random, monthStart, referenceDate);

                    var order = new Pedido {
                        ClienteId = customer.Id,
                        Cliente = customer,
                        EnderecoId = address.Id,
                        Endereco = address,
                        Data = orderDate,
                        Total = decimal.Round(total, 2),
                        TipoEntrega = deliveryType,
                        DataEntregaPrevista = deliveryType == "PROGRAMADA"
                            ? orderDate.Date.AddDays(random.Next(5, 18))
                            : orderDate.Date.AddDays(random.Next(3, 11)),
                        Status = status,
                        Itens = orderItems,
                        Pagamentos = new List<Pagamento> {
                            new() {
                                Metodo = PaymentMethods[random.Next(PaymentMethods.Length)],
                                Valor = decimal.Round(total, 2),
                                Status = "Aprovado"
                            }
                        }
                    };

                    _dataProvider.AddOrder(order);
                    ordersCreated++;
                    customersUsed.Add(customer.Id);
                }
            }

            await _dataProvider.SaveChangesAsync(cancellationToken);

            return AdminSalesHistorySeedResult.Success(ordersCreated, itemsCreated, unitsSold, customersUsed.Count, months);
        }

        private static DateTime GenerateRandomDate(Random random, DateTime monthStart, DateTime monthEnd) {
            var daySpan = Math.Max((monthEnd - monthStart).Days, 0);
            var randomDay = monthStart.AddDays(random.Next(0, daySpan + 1));
            return randomDay
                .AddHours(random.Next(8, 21))
                .AddMinutes(random.Next(0, 60))
                .AddSeconds(random.Next(0, 60));
        }

        private static string PickOrderStatus(Random random, DateTime monthStart, DateTime referenceDate) {
            var monthDistance = ((referenceDate.Year - monthStart.Year) * 12) + referenceDate.Month - monthStart.Month;

            if (monthDistance >= 2) {
                return random.Next(100) < 70 ? "ENTREGUE" : "EM TRANSPORTE";
            }

            if (monthDistance == 1) {
                return random.Next(100) < 55 ? "ENTREGUE" : "EM TRANSPORTE";
            }

            return EligibleOrderStatuses[random.Next(EligibleOrderStatuses.Length)];
        }

        private static void EnsureStockRecords(IEnumerable<Livro> books) {
            foreach (var book in books) {
                if (book.Estoque != null) {
                    continue;
                }

                book.Estoque = new Estoque {
                    LivroId = book.Id,
                    Livro = book,
                    Quantidade = 0,
                    QuantidadeMinima = 10
                };
            }
        }

        private static void EnsureStockAvailable(Livro book, int quantityNeeded, Random random) {
            if (book.Estoque.Quantidade >= quantityNeeded) {
                return;
            }

            var replenishment = quantityNeeded + random.Next(12, 40);
            book.Estoque.Quantidade += replenishment;
        }
    }
}
