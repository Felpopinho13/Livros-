using System.Text.Json;
using Livros.Web.Models.Chatbot;
using Livros.Web.Services;
using Microsoft.AspNetCore.Mvc;

public class ChatbotController : Controller {
    private const string SessionKey = "ChatbotState";
    private readonly LivroRecommendationChatService _chatService;

    public ChatbotController(LivroRecommendationChatService chatService) {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<IActionResult> Recommend([FromBody] ChatbotRequest request, CancellationToken cancellationToken) {
        if (request == null || string.IsNullOrWhiteSpace(request.Message)) {
            return BadRequest(new { error = "Mensagem obrigatoria." });
        }

        var state = LoadState();
        var clienteId = GetClienteId();
        var response = await _chatService.RecommendAsync(request.Message, clienteId, state, cancellationToken);
        SaveState(state);

        return Json(response);
    }

    private int? GetClienteId() {
        var clienteId = HttpContext.Session.GetString("ClienteId");
        return int.TryParse(clienteId, out var value) ? value : null;
    }

    private ChatbotSessionState LoadState() {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json)) {
            return new ChatbotSessionState();
        }

        return JsonSerializer.Deserialize<ChatbotSessionState>(json) ?? new ChatbotSessionState();
    }

    private void SaveState(ChatbotSessionState state) {
        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(state));
    }
}
