using System.Text.Json;
using Livros.Application.Recommendations;

namespace Livros.Web.Services;

public sealed class ChatbotSessionService {
    private const string SessionKey = "ChatbotState";

    public ChatbotSessionState LoadState(ISession session) {
        var json = session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json)) {
            return new ChatbotSessionState();
        }

        return JsonSerializer.Deserialize<ChatbotSessionState>(json) ?? new ChatbotSessionState();
    }

    public void SaveState(ISession session, ChatbotSessionState state) {
        session.SetString(SessionKey, JsonSerializer.Serialize(state));
    }
}
