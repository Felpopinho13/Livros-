namespace Livros.Application.AdminCustomers {
    public sealed class AdminCustomersService {
        private static readonly string[] EligibleStatuses = {
            "APROVADA",
            "PAGAMENTO APROVADO",
            "EM SEPARACAO",
            "EM TRANSPORTE",
            "ENVIADO",
            "ENTREGUE"
        };

        private const int PageSize = 10;
        private readonly IAdminCustomersDataProvider _dataProvider;

        public AdminCustomersService(IAdminCustomersDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public async Task<AdminCustomersResult> BuildAsync(AdminCustomersQuery query, CancellationToken cancellationToken = default) {
            var normalizedQuery = new AdminCustomersQuery {
                Busca = query.Busca,
                Nome = query.Nome,
                Email = query.Email,
                Cpf = query.Cpf,
                Telefone = query.Telefone,
                Genero = query.Genero,
                DataNascimento = query.DataNascimento,
                Status = query.Status,
                Admin = query.Admin,
                Pagina = Math.Max(query.Pagina, 1)
            };

            var pageData = await _dataProvider.LoadPageAsync(normalizedQuery, PageSize, cancellationToken);
            var clienteIds = pageData.Clientes.Select(c => c.Id).ToList();
            var valoresElegiveis = await _dataProvider.LoadEligibleTotalsAsync(clienteIds, cancellationToken);

            return new AdminCustomersResult {
                Clientes = pageData.Clientes,
                ValoresElegiveisPorCliente = valoresElegiveis,
                PaginaAtual = normalizedQuery.Pagina,
                TotalPaginas = Math.Max(1, (int)Math.Ceiling((double)pageData.TotalClientes / PageSize))
            };
        }

        public async Task<AdminCustomerTransactionsResult?> BuildTransactionsAsync(int clienteId, CancellationToken cancellationToken = default) {
            var data = await _dataProvider.LoadTransactionsAsync(clienteId, cancellationToken);
            if (data.Cliente == null) {
                return null;
            }

            return new AdminCustomerTransactionsResult {
                Cliente = data.Cliente,
                Pedidos = data.Pedidos,
                Trocas = data.Trocas,
                Cupons = data.Cupons,
                ValorElegivelRanking = decimal.Round(
                    data.Pedidos.Where(p => StatusContaParaRanking(p.Status)).Sum(p => p.Total),
                    2)
            };
        }

        private static bool StatusContaParaRanking(string? status) {
            return !string.IsNullOrWhiteSpace(status)
                && EligibleStatuses.Contains(status.Trim().ToUpperInvariant());
        }
    }
}
