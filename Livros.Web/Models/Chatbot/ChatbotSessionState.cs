namespace Livros.Web.Models.Chatbot {
    public sealed class ChatbotSessionState {
        public List<ChatbotTurn> Turns { get; set; } = new();
        public List<int> LastRecommendedBookIds { get; set; } = new();
    }

    public sealed class ChatbotTurn {
        public string Role { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
