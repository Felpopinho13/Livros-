using System.Text.Json;
using Livros.Application.CustomerCart;

namespace Livros.Web.Services;

public sealed class CartSessionService {
    public const string CartSessionKey = "Carrinho";

    public string EnsureReservationSessionKey(ISession session) {
        _ = session.Id;
        return session.Id;
    }

    public List<CustomerCartItemEntry> LoadCart(ISession session) {
        var cartJson = LoadCartJson(session);
        if (string.IsNullOrWhiteSpace(cartJson)) {
            return new List<CustomerCartItemEntry>();
        }

        return JsonSerializer.Deserialize<List<CustomerCartItemEntry>>(cartJson) ?? new List<CustomerCartItemEntry>();
    }

    public string? LoadCartJson(ISession session) {
        return session.GetString(CartSessionKey);
    }

    public void SaveCart(ISession session, List<CustomerCartItemEntry> items) {
        if (items == null || !items.Any()) {
            session.Remove(CartSessionKey);
            return;
        }

        SaveCartJson(session, JsonSerializer.Serialize(items));
    }

    public void SaveCartJson(ISession session, string? cartJson) {
        if (string.IsNullOrWhiteSpace(cartJson)) {
            session.Remove(CartSessionKey);
            return;
        }

        session.SetString(CartSessionKey, cartJson);
    }

    public void ClearCart(ISession session) {
        session.Remove(CartSessionKey);
    }
}
