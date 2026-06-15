namespace Livros.Application.Recommendations {
    public sealed class ChatbotResponse {
        public string Reply { get; set; } = string.Empty;
        public bool UsedAi { get; set; }
        public string Source { get; set; } = "fallback";
        public List<RecommendedBookDto> Recommendations { get; set; } = new();
    }

    public sealed class RecommendedBookDto {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
    }

    public sealed class ChatbotSessionState {
        public List<ChatbotTurn> Turns { get; set; } = new();
        public List<int> LastRecommendedBookIds { get; set; } = new();
    }

    public sealed class ChatbotTurn {
        public string Role { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class LivroRecommendationAiOptions {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-5.4-mini";
    }
}
