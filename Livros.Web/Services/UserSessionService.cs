namespace Livros.Web.Services;

public sealed class UserSessionService {
    public const string UserEmailKey = "Usuario";
    public const string IsAdminKey = "IsAdmin";
    public const string CustomerIdKey = "ClienteId";

    public string? GetUserEmail(ISession session) {
        return session.GetString(UserEmailKey);
    }

    public int? GetCustomerId(ISession session) {
        var customerId = session.GetString(CustomerIdKey);
        return int.TryParse(customerId, out var value) ? value : null;
    }

    public bool IsAdmin(ISession session) {
        var rawValue = session.GetString(IsAdminKey);
        return bool.TryParse(rawValue, out var isAdmin) && isAdmin;
    }

    public void SignIn(ISession session, string email, bool isAdmin, int customerId) {
        session.SetString(UserEmailKey, email);
        session.SetString(IsAdminKey, isAdmin.ToString());
        session.SetString(CustomerIdKey, customerId.ToString());
    }

    public void UpdateEmail(ISession session, string email) {
        session.SetString(UserEmailKey, email);
    }

    public void Clear(ISession session) {
        session.Clear();
    }
}
