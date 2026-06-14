using Livros.Application.SalesAnalysis;

namespace Livros.Web.Models.ViewModels {
    public static class AdminAnaliseVendasViewModelMapper {
        public static AdminAnaliseVendasViewModel Map(SalesAnalysisResult analysis) {
            return new AdminAnaliseVendasViewModel {
                DataInicio = analysis.DataInicio,
                DataFim = analysis.DataFim,
                Agrupamento = analysis.Agrupamento,
                TotalPedidos = analysis.TotalPedidos,
                TotalItensVendidos = analysis.TotalItensVendidos,
                ReceitaTotal = analysis.ReceitaTotal,
                TicketMedio = analysis.TicketMedio,
                QuantidadeProdutosComparados = analysis.QuantidadeProdutosComparados,
                QuantidadeCategoriasComparadas = analysis.QuantidadeCategoriasComparadas,
                EvolucaoPeriodo = analysis.EvolucaoPeriodo
                    .Select(item => new AdminAnalisePeriodoItemViewModel {
                        Rotulo = item.Rotulo,
                        Receita = item.Receita,
                        Pedidos = item.Pedidos
                    })
                    .ToList(),
                Produtos = analysis.Produtos
                    .Select(item => new AdminAnaliseProdutoItemViewModel {
                        Titulo = item.Titulo,
                        UnidadesVendidas = item.UnidadesVendidas,
                        Pedidos = item.Pedidos,
                        Receita = item.Receita
                    })
                    .ToList(),
                Categorias = analysis.Categorias
                    .Select(item => new AdminAnaliseCategoriaItemViewModel {
                        CategoriaId = item.CategoriaId,
                        Nome = item.Nome,
                        UnidadesVendidas = item.UnidadesVendidas,
                        Pedidos = item.Pedidos,
                        Receita = item.Receita
                    })
                    .ToList(),
                CategoriasSelecionadas = analysis.CategoriasSelecionadas,
                CategoriasDisponiveis = analysis.CategoriasDisponiveis
                    .Select(item => new AdminAnaliseCategoriaOptionViewModel {
                        Id = item.Id,
                        Nome = item.Nome
                    })
                    .ToList(),
                GraficoCategorias = analysis.GraficoCategorias
                    .Select(item => new AdminAnaliseCategoriaLinhaViewModel {
                        CategoriaId = item.CategoriaId,
                        Nome = item.Nome,
                        Cor = item.Cor,
                        Valores = item.Valores
                    })
                    .ToList()
            };
        }
    }
}
