using Livros.Domain;
using Livros.Web.Helpers;
using Livros.Application.AdminCustomers;

namespace Livros.Web.Models.ViewModels {
    public sealed class AdminClientesViewModel {
        public List<Cliente> Clientes { get; init; } = new();
        public Dictionary<int, ClienteRankingInfo> RankingsPorCliente { get; init; } = new();
        public int PaginaAtual { get; init; }
        public int TotalPaginas { get; init; }
    }

    public static class AdminClientesViewModelMapper {
        public static AdminClientesViewModel Map(AdminCustomersResult result) {
            return new AdminClientesViewModel {
                Clientes = result.Clientes,
                RankingsPorCliente = result.Clientes.ToDictionary(
                    c => c.Id,
                    c => ClienteRankingHelper.ObterRanking(
                        result.ValoresElegiveisPorCliente.TryGetValue(c.Id, out var total) ? total : 0m)),
                PaginaAtual = result.PaginaAtual,
                TotalPaginas = result.TotalPaginas
            };
        }
    }
}
