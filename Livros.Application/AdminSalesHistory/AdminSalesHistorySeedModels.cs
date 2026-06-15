using Livros.Domain;

namespace Livros.Application.AdminSalesHistory {
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

    public sealed record AdminSalesSeedGeography(Estado State, Cidade City, Bairro Neighborhood);
}
