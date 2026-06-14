using System.Globalization;
using Livros.Application.AdminCustomers;
using Livros.Domain;
using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public sealed class AdminCustomersDataProvider : IAdminCustomersDataProvider {
        private static readonly string[] EligibleStatuses = {
            "APROVADA",
            "PAGAMENTO APROVADO",
            "EM SEPARACAO",
            "EM TRANSPORTE",
            "ENVIADO",
            "ENTREGUE"
        };

        private readonly AppDbContext _context;

        public AdminCustomersDataProvider(AppDbContext context) {
            _context = context;
        }

        public async Task<AdminCustomersPageData> LoadPageAsync(AdminCustomersQuery query, int pageSize, CancellationToken cancellationToken = default) {
            var filteredQuery = ApplyFilters(_context.Clientes.AsNoTracking(), query);
            var totalClientes = await filteredQuery.CountAsync(cancellationToken);
            var clientes = await filteredQuery
                .OrderBy(c => c.Id)
                .Skip((query.Pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new AdminCustomersPageData {
                Clientes = clientes,
                TotalClientes = totalClientes
            };
        }

        public async Task<Dictionary<int, decimal>> LoadEligibleTotalsAsync(IReadOnlyCollection<int> clienteIds, CancellationToken cancellationToken = default) {
            if (clienteIds.Count == 0) {
                return new Dictionary<int, decimal>();
            }

            return await _context.Pedidos
                .AsNoTracking()
                .Where(p => clienteIds.Contains(p.ClienteId) && EligibleStatuses.Contains(p.Status))
                .GroupBy(p => p.ClienteId)
                .Select(g => new {
                    ClienteId = g.Key,
                    Total = g.Sum(p => p.Total)
                })
                .ToDictionaryAsync(x => x.ClienteId, x => decimal.Round(x.Total, 2), cancellationToken);
        }


        public async Task<AdminCustomerTransactionsData> LoadTransactionsAsync(int clienteId, CancellationToken cancellationToken = default) {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clienteId, cancellationToken);

            if (cliente == null) {
                return new AdminCustomerTransactionsData();
            }

            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.ClienteId == clienteId)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Livro)
                .Include(p => p.Pagamentos)
                .OrderByDescending(p => p.Data)
                .ToListAsync(cancellationToken);

            var trocas = await _context.Trocas
                .AsNoTracking()
                .Where(t => t.ClienteId == clienteId)
                .Include(t => t.PedidoItem)
                    .ThenInclude(i => i.Livro)
                .OrderByDescending(t => t.DataSolicitacao)
                .ToListAsync(cancellationToken);

            var cupons = await _context.CuponsDesconto
                .AsNoTracking()
                .Where(c => c.ClienteId == clienteId)
                .OrderByDescending(c => c.DataCriacao)
                .ToListAsync(cancellationToken);

            return new AdminCustomerTransactionsData {
                Cliente = cliente,
                Pedidos = pedidos,
                Trocas = trocas,
                Cupons = cupons
            };
        }
        private static IQueryable<Cliente> ApplyFilters(IQueryable<Cliente> query, AdminCustomersQuery filters) {
            if (!string.IsNullOrWhiteSpace(filters.Busca)) {
                query = query.Where(c =>
                    c.Nome.Contains(filters.Busca) ||
                    c.Email.Contains(filters.Busca) ||
                    (c.CPF != null && c.CPF.Contains(filters.Busca)) ||
                    (c.Telefone != null && c.Telefone.Contains(filters.Busca)));
            }

            if (!string.IsNullOrWhiteSpace(filters.Nome)) {
                query = query.Where(c => c.Nome.Contains(filters.Nome));
            }

            if (!string.IsNullOrWhiteSpace(filters.Email)) {
                query = query.Where(c => c.Email.Contains(filters.Email));
            }

            if (!string.IsNullOrWhiteSpace(filters.Cpf)) {
                query = query.Where(c => c.CPF != null && c.CPF.Contains(filters.Cpf));
            }

            if (!string.IsNullOrWhiteSpace(filters.Telefone)) {
                query = query.Where(c => c.Telefone != null && c.Telefone.Contains(filters.Telefone));
            }

            if (!string.IsNullOrWhiteSpace(filters.Genero)) {
                query = query.Where(c => c.Genero != null && c.Genero == filters.Genero);
            }

            if (!string.IsNullOrWhiteSpace(filters.DataNascimento)
                && DateTime.TryParseExact(filters.DataNascimento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataNascimentoFiltro)) {
                var inicioDia = dataNascimentoFiltro.Date;
                var fimDia = inicioDia.AddDays(1);
                query = query.Where(c => c.DataNascimento.HasValue
                    && c.DataNascimento.Value >= inicioDia
                    && c.DataNascimento.Value < fimDia);
            }

            if (!string.IsNullOrWhiteSpace(filters.Status)) {
                if (filters.Status == "ativo") {
                    query = query.Where(c => c.IsAtivo);
                }
                else if (filters.Status == "inativo") {
                    query = query.Where(c => !c.IsAtivo);
                }
            }

            if (!string.IsNullOrWhiteSpace(filters.Admin) && bool.TryParse(filters.Admin, out var isAdmin)) {
                query = query.Where(c => c.IsAdmin == isAdmin);
            }

            return query;
        }
    }
}
