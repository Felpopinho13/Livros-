using Livros.Domain;

namespace Livros.Application.CustomerIdentity {
    public sealed class CustomerLoginResult {
        public bool Authenticated { get; init; }
        public Cliente? Customer { get; init; }
    }
}
