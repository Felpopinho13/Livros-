using Livros.Domain;

namespace Livros.Web.Helpers {
    public sealed class ClienteRankingInfo {
        public string Nome { get; init; } = "Bronze";
        public string CssClass { get; init; } = "bronze";
        public decimal ValorElegivel { get; init; }
        public decimal? ProximoMarco { get; init; }
        public string? ProximoNome { get; init; }
    }

    public static class ClienteRankingHelper {
        private static readonly string[] StatusElegiveis = {
            "PAGAMENTO APROVADO",
            "EM SEPARACAO",
            "ENVIADO",
            "ENTREGUE"
        };

        public static bool StatusContaParaRanking(string? status) {
            return !string.IsNullOrWhiteSpace(status)
                && StatusElegiveis.Contains(status.Trim().ToUpperInvariant());
        }

        public static decimal CalcularValorElegivel(IEnumerable<Pedido> pedidos) {
            return decimal.Round(
                pedidos.Where(p => StatusContaParaRanking(p.Status))
                      .Sum(p => p.Total),
                2);
        }

        public static ClienteRankingInfo ObterRanking(decimal valorElegivel) {
            if (valorElegivel >= 1000m) {
                return new ClienteRankingInfo {
                    Nome = "Diamante",
                    CssClass = "diamante",
                    ValorElegivel = valorElegivel
                };
            }

            if (valorElegivel >= 500m) {
                return new ClienteRankingInfo {
                    Nome = "Ouro",
                    CssClass = "ouro",
                    ValorElegivel = valorElegivel,
                    ProximoMarco = 1000m,
                    ProximoNome = "Diamante"
                };
            }

            if (valorElegivel >= 200m) {
                return new ClienteRankingInfo {
                    Nome = "Prata",
                    CssClass = "prata",
                    ValorElegivel = valorElegivel,
                    ProximoMarco = 500m,
                    ProximoNome = "Ouro"
                };
            }

            return new ClienteRankingInfo {
                Nome = "Bronze",
                CssClass = "bronze",
                ValorElegivel = valorElegivel,
                ProximoMarco = 200m,
                ProximoNome = "Prata"
            };
        }
    }
}
