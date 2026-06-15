namespace Livros.Web.Models.Chatbot {
    public sealed class ChatbotResponse {
        public string Reply { get; set; } = string.Empty;
        public bool UsedAi { get; set; }
        public string Source { get; set; } = "fallback";
        public List<ChatbotRecommendationItem> Recommendations { get; set; } = new();
    }

    public sealed class ChatbotRecommendationItem {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string DetailsUrl { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
    }
}
