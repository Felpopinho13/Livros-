using Livros.Domain;

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

        public AdminCustomerOperationResult Create(AdminCustomerCreateCommand command) {
            if (string.IsNullOrWhiteSpace(command.Cliente.Nome)) {
                return new AdminCustomerOperationResult {
                    Succeeded = false,
                    Message = "Nome é obrigatório."
                };
            }

            if (string.IsNullOrWhiteSpace(command.Cliente.Email)) {
                return new AdminCustomerOperationResult {
                    Succeeded = false,
                    Message = "Email é obrigatório."
                };
            }

            if (string.IsNullOrWhiteSpace(command.Cliente.Senha)) {
                return new AdminCustomerOperationResult {
                    Succeeded = false,
                    Message = "Senha é obrigatória."
                };
            }

            if (!string.IsNullOrEmpty(command.Cliente.CPF)) {
                command.Cliente.CPF = command.Cliente.CPF.Replace(".", "").Replace("-", "");
            }

            command.Cliente.IsAtivo = true;

            _dataProvider.AddCustomer(command.Cliente);
            _dataProvider.SaveChanges();

            return new AdminCustomerOperationResult {
                Succeeded = true,
                Message = "Cliente criado com sucesso!"
            };
        }

        public AdminCustomerOperationResult UpdateStatus(AdminCustomerStatusCommand command) {
            var cliente = _dataProvider.LoadCustomerById(command.ClienteId);
            if (cliente == null) {
                return new AdminCustomerOperationResult {
                    Succeeded = false,
                    Message = "Cliente nao encontrado."
                };
            }

            cliente.IsAtivo = command.IsAtivo;
            _dataProvider.SaveChanges();

            return new AdminCustomerOperationResult {
                Succeeded = true,
                Message = command.IsAtivo ? "Cliente ativado com sucesso!" : "Cliente desativado com sucesso!"
            };
        }

        public AdminCustomerOperationResult Update(AdminCustomerUpdateCommand command) {
            var clienteDb = _dataProvider.LoadCustomerById(command.Cliente.Id);
            if (clienteDb == null) {
                return new AdminCustomerOperationResult {
                    Succeeded = false,
                    Message = "Cliente nao encontrado."
                };
            }

            clienteDb.Nome = command.Cliente.Nome;
            clienteDb.Email = command.Cliente.Email;
            clienteDb.CPF = command.Cliente.CPF;
            clienteDb.Telefone = command.Cliente.Telefone;
            clienteDb.Genero = command.Cliente.Genero;
            clienteDb.DataNascimento = command.Cliente.DataNascimento;
            clienteDb.IsAdmin = command.Cliente.IsAdmin;

            _dataProvider.SaveChanges();

            return new AdminCustomerOperationResult {
                Succeeded = true,
                Message = "Cliente atualizado com sucesso!"
            };
        }

        public AdminCustomerOperationResult Delete(AdminCustomerDeletionCommand command) {
            var cliente = _dataProvider.LoadCustomerByIdWithAddressesAndCards(command.ClienteId);
            if (cliente == null) {
                return new AdminCustomerOperationResult {
                    Succeeded = false,
                    Message = "Cliente nao encontrado."
                };
            }

            if (cliente.Enderecos != null && cliente.Enderecos.Count > 0) {
                _dataProvider.RemoveAddresses(cliente.Enderecos);
            }

            if (cliente.Cartoes != null && cliente.Cartoes.Count > 0) {
                _dataProvider.RemoveCards(cliente.Cartoes);
            }

            _dataProvider.RemoveCustomer(cliente);
            _dataProvider.SaveChanges();

            return new AdminCustomerOperationResult {
                Succeeded = true,
                Message = "Cliente removido com sucesso!"
            };
        }

        private static bool StatusContaParaRanking(string? status) {
            return !string.IsNullOrWhiteSpace(status)
                && EligibleStatuses.Contains(status.Trim().ToUpperInvariant());
        }
    }
}
