using Livros.Domain;

namespace Livros.Application.CustomerIdentity {
    public sealed class CustomerIdentityService {
        private readonly ICustomerIdentityDataProvider _dataProvider;

        public CustomerIdentityService(ICustomerIdentityDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public List<Cliente> ListActiveCustomers() {
            return _dataProvider.LoadActiveCustomers();
        }

        public CustomerLoginResult Authenticate(string email, string password) {
            var customer = _dataProvider.LoadActiveCustomerByEmail(email);
            if (customer == null) {
                return new CustomerLoginResult {
                    Authenticated = false
                };
            }

            var authenticated = _dataProvider.VerifyPassword(password, customer.Senha);
            return new CustomerLoginResult {
                Authenticated = authenticated,
                Customer = authenticated ? customer : null
            };
        }
    }
}
