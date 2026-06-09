using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Web.Services {
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

        private readonly AppDbContext _context;
        private readonly ILogger<AdminSalesHistorySeedService> _logger;

        public AdminSalesHistorySeedService(AppDbContext context, ILogger<AdminSalesHistorySeedService> logger) {
            _context = context;
            _logger = logger;
        }

        public async Task<AdminSalesHistorySeedResult> GenerateAsync(int months = 13, CancellationToken cancellationToken = default) {
            if (months < 13) {
                months = 13;
            }

            var random = new Random();
            var referenceDate = DateTime.Today;
            var books = await _context.Livros
                .Include(l => l.Categorias)
                .Include(l => l.Estoque)
                .Where(l => l.IsAtivo && l.Preco > 0)
                .OrderBy(l => l.Id)
                .ToListAsync(cancellationToken);

            if (books.Count < 3) {
                return AdminSalesHistorySeedResult.Fail("Cadastre pelo menos 3 livros ativos para gerar o historico de vendas.");
            }

            var geography = await EnsureGeographyAsync(cancellationToken);
            var customers = await EnsureDemoCustomersAsync(geography, cancellationToken);

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
                        DataEntregaPrevista = deliveryType == "PROGRAMADA" ? orderDate.Date.AddDays(random.Next(5, 18)) : orderDate.Date.AddDays(random.Next(3, 11)),
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

                    _context.Pedidos.Add(order);
                    ordersCreated++;
                    customersUsed.Add(customer.Id);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Historico de vendas demo gerado com sucesso. Pedidos: {Orders}, itens: {Items}, unidades: {Units}, clientes usados: {Customers}.",
                ordersCreated,
                itemsCreated,
                unitsSold,
                customersUsed.Count);

            return AdminSalesHistorySeedResult.Success(ordersCreated, itemsCreated, unitsSold, customersUsed.Count, months);
        }

        private async Task<SeedGeography> EnsureGeographyAsync(CancellationToken cancellationToken) {
            var state = await _context.Estados
                .OrderBy(e => e.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (state == null) {
                state = new Estado {
                    Nome = "Sao Paulo",
                    Sigla = "SP",
                    Cidades = new List<Cidade>()
                };
                _context.Estados.Add(state);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var city = await _context.Cidades
                .Where(c => c.EstadoId == state.Id)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (city == null) {
                city = new Cidade {
                    Nome = "Sao Paulo",
                    EstadoId = state.Id,
                    Estado = state,
                    Bairros = new List<Bairro>()
                };
                _context.Cidades.Add(city);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var neighborhood = await _context.Bairros
                .Where(b => b.CidadeId == city.Id)
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (neighborhood == null) {
                neighborhood = new Bairro {
                    Nome = "Centro",
                    CidadeId = city.Id,
                    Cidade = city
                };
                _context.Bairros.Add(neighborhood);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new SeedGeography(state, city, neighborhood);
        }

        private async Task<List<Cliente>> EnsureDemoCustomersAsync(SeedGeography geography, CancellationToken cancellationToken) {
            var customers = await _context.Clientes
                .Include(c => c.Enderecos)
                .Where(c => c.IsAtivo && !c.IsAdmin)
                .OrderBy(c => c.Id)
                .Take(12)
                .ToListAsync(cancellationToken);

            if (customers.Count < 6) {
                var needed = 6 - customers.Count;
                var existingEmails = customers
                    .Select(c => c.Email)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                for (var index = 0; index < DemoCustomerNames.Length && needed > 0; index++) {
                    var email = $"demo.vendas.{index + 1}@livros.local";
                    if (existingEmails.Contains(email)) {
                        continue;
                    }

                    var customer = new Cliente {
                        Nome = DemoCustomerNames[index],
                        Email = email,
                        Senha = BCrypt.Net.BCrypt.HashPassword("123456"),
                        CPF = $"{randomDigits(index + 1)}",
                        Telefone = $"1199{(100000 + index):D6}",
                        Genero = index % 2 == 0 ? "Feminino" : "Masculino",
                        DataNascimento = DateTime.Today.AddYears(-(22 + index)).AddDays(index * 7),
                        IsAtivo = true,
                        Enderecos = new List<Endereco>()
                    };

                    _context.Clientes.Add(customer);
                    customers.Add(customer);
                    existingEmails.Add(email);
                    needed--;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            foreach (var customer in customers) {
                if (customer.Enderecos != null && customer.Enderecos.Any()) {
                    continue;
                }

                customer.Enderecos ??= new List<Endereco>();
                customer.Enderecos.Add(new Endereco {
                    NomeEndereco = "Principal",
                    CEP = "01001000",
                    TipoLogradouro = "Rua",
                    Logradouro = "Rua da Demo",
                    Numero = ((customer.Id % 400) + 100).ToString(),
                    Complemento = "Apto 1",
                    TipoResidencia = "Casa",
                    Pais = "Brasil",
                    IsPadrao = true,
                    IsEntrega = true,
                    IsCobranca = true,
                    CidadeId = geography.City.Id,
                    Cidade = geography.City,
                    BairroId = geography.Neighborhood.Id,
                    Bairro = geography.Neighborhood,
                    ClienteId = customer.Id,
                    Cliente = customer
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return await _context.Clientes
                .Include(c => c.Enderecos)
                .Where(c => c.IsAtivo && !c.IsAdmin)
                .OrderBy(c => c.Id)
                .Take(12)
                .ToListAsync(cancellationToken);

            static string randomDigits(int seed) {
                return $"{seed:D11}";
            }
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

        private sealed record SeedGeography(Estado State, Cidade City, Bairro Neighborhood);
    }

    public sealed class AdminSalesHistorySeedResult {
        public bool Succeeded { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public int OrdersCreated { get; private init; }
        public int ItemsCreated { get; private init; }
        public int UnitsSold { get; private init; }
        public int CustomersUsed { get; private init; }
        public int MonthsCovered { get; private init; }

        public static AdminSalesHistorySeedResult Success(int ordersCreated, int itemsCreated, int unitsSold, int customersUsed, int monthsCovered) {
            return new AdminSalesHistorySeedResult {
                Succeeded = true,
                OrdersCreated = ordersCreated,
                ItemsCreated = itemsCreated,
                UnitsSold = unitsSold,
                CustomersUsed = customersUsed,
                MonthsCovered = monthsCovered,
                Message = $"Historico gerado com sucesso: {ordersCreated} pedido(s), {itemsCreated} item(ns), {unitsSold} unidade(s) vendida(s), {customersUsed} cliente(s) e {monthsCovered} mes(es) cobertos."
            };
        }

        public static AdminSalesHistorySeedResult Fail(string message) {
            return new AdminSalesHistorySeedResult {
                Succeeded = false,
                Message = message
            };
        }
    }
}
