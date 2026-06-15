using Livros.Application.AdminSalesHistory;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class AdminSalesHistorySeedDataProvider : IAdminSalesHistorySeedDataProvider {
        private readonly AppDbContext _context;

        public AdminSalesHistorySeedDataProvider(AppDbContext context) {
            _context = context;
        }

        public async Task<List<Livro>> LoadEligibleBooksAsync(CancellationToken cancellationToken = default) {
            return await _context.Livros
                .Include(l => l.Categorias)
                .Include(l => l.Estoque)
                .Where(l => l.IsAtivo && l.Preco > 0)
                .OrderBy(l => l.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<AdminSalesSeedGeography> EnsureGeographyAsync(CancellationToken cancellationToken = default) {
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

            return new AdminSalesSeedGeography(state, city, neighborhood);
        }

        public async Task<List<Cliente>> EnsureCustomersAsync(AdminSalesSeedGeography geography, IReadOnlyCollection<string> demoCustomerNames, CancellationToken cancellationToken = default) {
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

                var names = demoCustomerNames.ToList();
                for (var index = 0; index < names.Count && needed > 0; index++) {
                    var email = $"demo.vendas.{index + 1}@livros.local";
                    if (existingEmails.Contains(email)) {
                        continue;
                    }

                    var customer = new Cliente {
                        Nome = names[index],
                        Email = email,
                        Senha = BCrypt.Net.BCrypt.HashPassword("123456"),
                        CPF = $"{RandomDigits(index + 1)}",
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
        }

        public void AddOrder(Pedido pedido) {
            _context.Pedidos.Add(pedido);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private static string RandomDigits(int seed) {
            return $"{seed:D11}";
        }
    }
}
