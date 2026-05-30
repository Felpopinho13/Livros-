namespace Livros.Web.Configuration {
    public sealed class OpenAiOptions {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-5.4-mini";
    }
}
