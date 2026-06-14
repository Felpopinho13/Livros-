namespace Livros.Application.SalesAnalysis {
    public sealed class SalesAnalysisQuery {
        public DateTime? DataInicio { get; init; }
        public DateTime? DataFim { get; init; }
        public IReadOnlyCollection<int>? CategoriasIds { get; init; }
        public string? Agrupamento { get; init; }
    }
}
