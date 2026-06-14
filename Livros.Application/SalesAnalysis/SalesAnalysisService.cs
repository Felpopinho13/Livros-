namespace Livros.Application.SalesAnalysis {
    public sealed class SalesAnalysisService {
        private readonly ISalesAnalysisDataProvider _dataProvider;

        public SalesAnalysisService(ISalesAnalysisDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public async Task<SalesAnalysisResult> BuildAsync(SalesAnalysisQuery query, CancellationToken cancellationToken = default) {
            var inicio = (query.DataInicio ?? DateTime.Today.AddMonths(-12).AddDays(1)).Date;
            var fim = (query.DataFim ?? DateTime.Today).Date;

            if (fim < inicio) {
                (inicio, fim) = (fim, inicio);
            }

            var categoriasDisponiveis = await _dataProvider.LoadCategoryOptionsAsync(cancellationToken);
            var itensVendidos = await _dataProvider.LoadSoldItemsAsync(inicio, fim, cancellationToken);

            return SalesAnalysisBuilder.Build(
                query.DataInicio,
                query.DataFim,
                query.CategoriasIds,
                query.Agrupamento,
                itensVendidos,
                categoriasDisponiveis);
        }
    }
}
