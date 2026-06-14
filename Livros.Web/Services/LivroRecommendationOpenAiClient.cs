using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Livros.Application.Recommendations;
using Livros.Web.Configuration;
using Livros.Web.Models.Chatbot;

namespace Livros.Web.Services {
    internal static class LivroRecommendationOpenAiClient {
        public static bool CanUse(OpenAiOptions options) {
            return !string.IsNullOrWhiteSpace(options.ApiKey);
        }

        public static async Task<string?> GenerateReplyAsync(
            HttpClient httpClient,
            OpenAiOptions options,
            ILogger logger,
            string message,
            IReadOnlyList<RecommendedBookDto> books,
            LivroRecommendationCustomerProfile customerProfile,
            ChatbotSessionState sessionState,
            CancellationToken cancellationToken) {
            var apiKey = options.ApiKey.Trim();
            if (string.IsNullOrWhiteSpace(apiKey)) {
                return null;
            }

            var previousTurns = string.Join(
                Environment.NewLine,
                sessionState.Turns
                    .TakeLast(6)
                    .Select(turn => $"{turn.Role}: {turn.Message}"));

            var recommendationLines = string.Join(
                Environment.NewLine,
                books.Select(book =>
                    $"- Id {book.Id} | Titulo: {book.Title} | Autor: {book.Author} | Categorias: {(book.Categories.Any() ? string.Join(", ", book.Categories) : "Sem categoria")} | Preco: {book.Price} | Motivo: {book.Reason}"));

            var historySummary = customerProfile.HasHistory
                ? $"Categorias mais compradas: {string.Join(", ", customerProfile.CategoryWeights.OrderByDescending(item => item.Value).Take(3).Select(item => item.Key))}. Autores recorrentes: {string.Join(", ", customerProfile.AuthorWeights.OrderByDescending(item => item.Value).Take(3).Select(item => item.Key))}."
                : "Cliente sem historico de compras identificado.";

            var systemPrompt = """
Voce e um assistente virtual de um e-commerce de livros.

Responda sempre em portugues do Brasil, em texto simples, sem Markdown e sem HTML.

Use apenas os livros fornecidos pelo sistema.
Nunca invente livros, autores, categorias, preco, estoque, sinopse ou disponibilidade.
Nao recomende livros fora da lista recebida.

Sua funcao e ajudar com:
- recomendacoes de livros;
- busca por titulo, autor ou categoria;
- preco e informacoes dos livros;
- disponibilidade dos livros do catalogo.

Sempre responda de forma educada e profissional.
Atente-se nas respostas anteriores da conversa.
Se voce exibiu uma lista antes e o usuario pedir o ultimo livro da lista ou mais opcoes, considere o contexto recente.

Se o usuario pedir algo fora do contexto de livros, responda educadamente que so pode ajudar com livros do catalogo.

Avalie o sentido do pedido antes de recomendar.
Se o usuario associar a leitura a uma situacao inadequada, improvavel, perigosa, inviavel ou sem sentido, nao aceite o pedido literalmente.
Explique de forma curta que aquele contexto nao e apropriado para leitura ou que o objetivo nao combina com o tipo de livro pedido.
Depois, se fizer sentido, redirecione para uma recomendacao segura e coerente dentro do catalogo, sem dizer que o livro resolve algo impossivel.

Se o pedido combinar autor, categoria, titulo ou tema com um objetivo inadequado, nao finja que os livros atendem ao objetivo so porque combinam com parte da busca.
Nesse caso, deixe claro o limite da recomendacao e, se couber, ofereca livros relacionados apenas a parte literaria valida do pedido.

Se o usuario pedir para listar livros, apresente no maximo 5.
Se o usuario pedir recomendacoes, recomende de 1 a 3 livros.
Explique brevemente o motivo da recomendacao.
Evite repetir livros ja recomendados quando o usuario pedir mais opcoes.
Nao recomende livros que o cliente ja comprou quando houver alternativas boas no catalogo.
""";

            var userPrompt = $"""
Pergunta do usuario:
{message}

Historico resumido do cliente:
{historySummary}

Ultimas mensagens da conversa:
{(string.IsNullOrWhiteSpace(previousTurns) ? "Sem historico anterior." : previousTurns)}

Livros candidatos do catalogo para voce analisar:
{recommendationLines}

Antes de responder, siga esta ordem mental:
1. Entenda o que o usuario realmente quer.
2. Verifique se o pedido faz sentido dentro do contexto de leitura e do catalogo.
3. Se houver uma parte impropria, perigosa, absurda ou impossivel no pedido, nao trate os livros como solucao literal.
4. Use os livros apenas se eles fizerem sentido como alternativa coerente dentro do catalogo.

Gere apenas a resposta final do assistente.
""";

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new OpenAiResponsesRequest {
                Model = options.Model,
                Input = new List<OpenAiInputMessage> {
                    OpenAiInputMessage.Create("system", systemPrompt),
                    OpenAiInputMessage.Create("user", userPrompt)
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode) {
                logger.LogWarning("OpenAI retornou status {StatusCode}: {Body}", response.StatusCode, content);
                return null;
            }

            var responseBody = JsonSerializer.Deserialize<OpenAiResponsesResponse>(content);
            return responseBody?.Output?
                .SelectMany(item => item.Content ?? new List<OpenAiOutputContent>())
                .Where(item => string.Equals(item.Type, "output_text", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.Type, "text", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        }

        private sealed class OpenAiResponsesRequest {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("input")]
            public List<OpenAiInputMessage> Input { get; set; } = new();
        }

        private sealed class OpenAiInputMessage {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public List<OpenAiInputText> Content { get; set; } = new();

            public static OpenAiInputMessage Create(string role, string text) {
                return new OpenAiInputMessage {
                    Role = role,
                    Content = new List<OpenAiInputText> {
                        new() {
                            Type = "input_text",
                            Text = text
                        }
                    }
                };
            }
        }

        private sealed class OpenAiInputText {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "input_text";

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        private sealed class OpenAiResponsesResponse {
            [JsonPropertyName("output")]
            public List<OpenAiOutputItem>? Output { get; set; }
        }

        private sealed class OpenAiOutputItem {
            [JsonPropertyName("content")]
            public List<OpenAiOutputContent>? Content { get; set; }
        }

        private sealed class OpenAiOutputContent {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}
