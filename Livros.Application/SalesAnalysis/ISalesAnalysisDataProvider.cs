using Livros.Domain;

namespace Livros.Application.SalesAnalysis {
    public interface ISalesAnalysisDataProvider {
        Task<List<SalesAnalysisCategoryOption>> LoadCategoryOptionsAsync(CancellationToken cancellationToken = default);
        Task<List<PedidoItem>> LoadSoldItemsAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);
    }
}
