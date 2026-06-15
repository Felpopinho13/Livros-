namespace Livros.Application.AdminOrders {
    public static class OrderStatusHelper {
        public static bool RequiresStockDecrease(string? status) {
            if (string.IsNullOrWhiteSpace(status)) {
                return false;
            }

            return status.Equals("APROVADA", StringComparison.OrdinalIgnoreCase)
                || status.Equals("PAGAMENTO APROVADO", StringComparison.OrdinalIgnoreCase)
                || status.Equals("EM SEPARACAO", StringComparison.OrdinalIgnoreCase)
                || status.Equals("EM TRANSPORTE", StringComparison.OrdinalIgnoreCase)
                || status.Equals("ENVIADO", StringComparison.OrdinalIgnoreCase)
                || status.Equals("ENTREGUE", StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<string> GetNextStatuses(string? statusAtual) {
            var status = NormalizeInternalStatus(statusAtual);

            return status switch {
                "APROVADA" => new[] { "EM SEPARACAO", "CANCELADO" },
                "EM SEPARACAO" => new[] { "EM TRANSPORTE", "CANCELADO" },
                "EM TRANSPORTE" => new[] { "ENTREGUE" },
                _ => Array.Empty<string>()
            };
        }

        public static string NormalizeInternalStatus(string? statusAtual) {
            return (statusAtual ?? string.Empty).Trim().ToUpperInvariant() switch {
                "EM PROCESSAMENTO" => "APROVADA",
                "PAGAMENTO APROVADO" => "APROVADA",
                "PAGAMENTO RECUSADO" => "REPROVADA",
                "ENVIADO" => "EM TRANSPORTE",
                var status => status
            };
        }

        public static string NormalizeDisplayStatus(string? statusAtual, string fallback = "NAO INFORMADO") {
            return NormalizeInternalStatus(statusAtual) switch {
                "APROVADA" => "APROVADA",
                "REPROVADA" => "REPROVADA",
                "EM SEPARACAO" => "EM SEPARACAO",
                "EM TRANSPORTE" => "EM TRANSPORTE",
                "ENTREGUE" => "ENTREGUE",
                "CANCELADO" => "CANCELADO",
                _ => statusAtual ?? fallback
            };
        }

        public static string[] GetEquivalentStatusesForFilter(string status) {
            return NormalizeInternalStatus(status) switch {
                "APROVADA" => new[] { "APROVADA", "PAGAMENTO APROVADO", "EM PROCESSAMENTO" },
                "REPROVADA" => new[] { "REPROVADA", "PAGAMENTO RECUSADO" },
                "EM TRANSPORTE" => new[] { "EM TRANSPORTE", "ENVIADO" },
                var statusNormalizado => new[] { statusNormalizado }
            };
        }
    }
}
