namespace Livros.Application.SalesAnalysis {
    public sealed class SalesAnalysisCategoryOption {
        public int Id { get; init; }
        public string Nome { get; init; } = string.Empty;
    }

    public sealed class SalesAnalysisPeriodItem {
        public string Rotulo { get; init; } = string.Empty;
        public decimal Receita { get; init; }
        public int Pedidos { get; init; }
    }

    public sealed class SalesAnalysisProductItem {
        public string Titulo { get; init; } = string.Empty;
        public int UnidadesVendidas { get; init; }
        public int Pedidos { get; init; }
        public decimal Receita { get; init; }
    }

    public sealed class SalesAnalysisCategoryItem {
        public int CategoriaId { get; init; }
        public string Nome { get; init; } = string.Empty;
        public int UnidadesVendidas { get; init; }
        public int Pedidos { get; init; }
        public decimal Receita { get; init; }
    }

    public sealed class SalesAnalysisCategoryLine {
        public int CategoriaId { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string Cor { get; init; } = string.Empty;
        public List<int> Valores { get; init; } = new();
    }

    public sealed class SalesAnalysisResult {
        public DateTime DataInicio { get; init; }
        public DateTime DataFim { get; init; }
        public string Agrupamento { get; init; } = "mensal";
        public int TotalPedidos { get; init; }
        public int TotalItensVendidos { get; init; }
        public decimal ReceitaTotal { get; init; }
        public decimal TicketMedio { get; init; }
        public int QuantidadeProdutosComparados { get; init; }
        public int QuantidadeCategoriasComparadas { get; init; }
        public List<SalesAnalysisPeriodItem> EvolucaoPeriodo { get; init; } = new();
        public List<SalesAnalysisProductItem> Produtos { get; init; } = new();
        public List<SalesAnalysisCategoryItem> Categorias { get; init; } = new();
        public List<int> CategoriasSelecionadas { get; init; } = new();
        public List<SalesAnalysisCategoryOption> CategoriasDisponiveis { get; init; } = new();
        public List<SalesAnalysisCategoryLine> GraficoCategorias { get; init; } = new();
    }
}
