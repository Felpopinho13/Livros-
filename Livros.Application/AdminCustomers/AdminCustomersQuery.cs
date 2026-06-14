namespace Livros.Application.AdminCustomers {
    public sealed class AdminCustomersQuery {
        public string? Busca { get; init; }
        public string? Nome { get; init; }
        public string? Email { get; init; }
        public string? Cpf { get; init; }
        public string? Telefone { get; init; }
        public string? Genero { get; init; }
        public string? DataNascimento { get; init; }
        public string? Status { get; init; }
        public string? Admin { get; init; }
        public int Pagina { get; init; } = 1;
    }
}
