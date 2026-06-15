using Livros.Domain;

namespace Livros.Application.AdminInventory {
    public sealed class AdminInventoryOperationResult {
        public bool Succeeded { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
