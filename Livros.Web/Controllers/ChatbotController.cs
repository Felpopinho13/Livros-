using Livros.Application.Recommendations;
using Livros.Web.Models.Chatbot;
using Microsoft.AspNetCore.Mvc;
using Livros.Web.Services;
using WebChatbotResponse = Livros.Web.Models.Chatbot.ChatbotResponse;

public class ChatbotController : Controller {
    private readonly LivroRecommendationChatService _chatService;
    private readonly ChatbotSessionService _chatbotSessionService;
    private readonly UserSessionService _userSessionService;

    public ChatbotController(LivroRecommendationChatService chatService, ChatbotSessionService chatbotSessionService, UserSessionService userSessionService) {
        _chatService = chatService;
        _chatbotSessionService = chatbotSessionService;
        _userSessionService = userSessionService;
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

        return Json(MapResponse(response));
    }

    private int? GetClienteId() {
        return _userSessionService.GetCustomerId(HttpContext.Session);
    }

    private ChatbotSessionState LoadState() {
        return _chatbotSessionService.LoadState(HttpContext.Session);
    }

    private void SaveState(ChatbotSessionState state) {
        _chatbotSessionService.SaveState(HttpContext.Session, state);
    }

    private static WebChatbotResponse MapResponse(Livros.Application.Recommendations.ChatbotResponse response) {
        return new WebChatbotResponse {
            Reply = response.Reply,
            UsedAi = response.UsedAi,
            Source = response.Source,
            Recommendations = response.Recommendations
                .Select(book => new ChatbotRecommendationItem {
                    Id = book.Id,
                    Title = book.Title,
                    Author = book.Author,
                    Price = $"R$ {book.Price:N2}",
                    ImageUrl = book.ImageUrl,
                    DetailsUrl = $"/Home/Detalhes/{book.Id}",
                    Reason = book.Reason,
                    Categories = book.Categories
                })
                .ToList()
        };
    }
}
