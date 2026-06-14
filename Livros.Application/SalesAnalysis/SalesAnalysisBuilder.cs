using Livros.Domain;

namespace Livros.Application.SalesAnalysis {
    public static class SalesAnalysisBuilder {
        public static SalesAnalysisResult Build(
            DateTime? dataInicio,
            DateTime? dataFim,
            IReadOnlyCollection<int>? categoriasIds,
            string? agrupamento,
            IReadOnlyList<PedidoItem> itensVendidos,
            IReadOnlyList<SalesAnalysisCategoryOption> categoriasDisponiveis) {
            var inicio = (dataInicio ?? DateTime.Today.AddMonths(-12).AddDays(1)).Date;
            var fim = (dataFim ?? DateTime.Today).Date;

            if (fim < inicio) {
                (inicio, fim) = (fim, inicio);
            }

            var agrupamentoNormalizado = NormalizeGrouping(agrupamento, inicio, fim);
            var pedidosFiltrados = itensVendidos
                .Select(i => i.Pedido)
                .Where(p => p != null)
                .GroupBy(p => p!.Id)
                .Select(g => g.First()!)
                .OrderBy(p => p.Data)
                .ToList();

            var periodos = GeneratePeriods(inicio, fim, agrupamentoNormalizado).ToList();
            var pedidosPorPeriodo = pedidosFiltrados
                .GroupBy(p => GetPeriodStart(p.Data.Date, agrupamentoNormalizado))
                .ToDictionary(g => g.Key, g => g.ToList());

            var evolucaoPeriodo = periodos
                .Select(periodo => {
                    pedidosPorPeriodo.TryGetValue(periodo, out var pedidosDoPeriodo);
                    pedidosDoPeriodo ??= new List<Pedido>();

                    return new SalesAnalysisPeriodItem {
                        Rotulo = FormatPeriodLabel(periodo, agrupamentoNormalizado),
                        Receita = decimal.Round(pedidosDoPeriodo.Sum(p => p.Total), 2),
                        Pedidos = pedidosDoPeriodo.Count
                    };
                })
                .ToList();

            var produtos = itensVendidos
                .GroupBy(i => new { i.LivroId, Titulo = i.Livro?.Titulo ?? "Livro" })
                .Select(g => new SalesAnalysisProductItem {
                    Titulo = g.Key.Titulo,
                    UnidadesVendidas = g.Sum(x => x.Quantidade),
                    Pedidos = g.Select(x => x.PedidoId).Distinct().Count(),
                    Receita = decimal.Round(g.Sum(x => x.PrecoUnitario * x.Quantidade), 2)
                })
                .OrderByDescending(x => x.Receita)
                .ThenByDescending(x => x.UnidadesVendidas)
                .ToList();

            var categoriaEventos = itensVendidos
                .SelectMany(i => {
                    var categoriasLivro = i.Livro?.Categorias != null && i.Livro.Categorias.Any()
                        ? i.Livro.Categorias.Select(c => new { c.Id, c.Nome })
                        : new[] { new { Id = 0, Nome = "Sem categoria" } };

                    return categoriasLivro.Select(categoria => new {
                        categoria.Id,
                        categoria.Nome,
                        i.Quantidade,
                        i.PedidoId,
                        Receita = i.PrecoUnitario * i.Quantidade,
                        Data = i.Pedido!.Data.Date
                    });
                })
                .ToList();

            var categorias = categoriaEventos
                .GroupBy(x => new { x.Id, x.Nome })
                .Select(g => new SalesAnalysisCategoryItem {
                    CategoriaId = g.Key.Id,
                    Nome = g.Key.Nome,
                    UnidadesVendidas = g.Sum(x => x.Quantidade),
                    Pedidos = g.Select(x => x.PedidoId).Distinct().Count(),
                    Receita = decimal.Round(g.Sum(x => x.Receita), 2)
                })
                .OrderByDescending(x => x.Receita)
                .ThenByDescending(x => x.UnidadesVendidas)
                .ToList();

            var categoriasSelecionadas = (categoriasIds ?? Array.Empty<int>())
                .Distinct()
                .Where(id => categoriasDisponiveis.Any(c => c.Id == id))
                .ToList();

            if (!categoriasSelecionadas.Any()) {
                categoriasSelecionadas = categorias
                    .Where(c => c.CategoriaId > 0)
                    .Take(5)
                    .Select(c => c.CategoriaId)
                    .ToList();
            }

            var eventosPorCategoriaPeriodo = categoriaEventos
                .Where(x => categoriasSelecionadas.Contains(x.Id))
                .GroupBy(x => new {
                    x.Id,
                    x.Nome,
                    Periodo = GetPeriodStart(x.Data, agrupamentoNormalizado)
                })
                .ToDictionary(
                    g => (g.Key.Id, g.Key.Periodo),
                    g => g.Sum(x => x.Quantidade));

            var nomesCategoriasSelecionadas = categoriasDisponiveis
                .Where(c => categoriasSelecionadas.Contains(c.Id))
                .OrderBy(c => categoriasSelecionadas.IndexOf(c.Id))
                .ToList();

            var coresGrafico = new[] {
                "#2563eb", "#f97316", "#7c3aed", "#16a34a", "#dc2626",
                "#0891b2", "#ea580c", "#4f46e5", "#ca8a04", "#db2777"
            };

            var graficoCategorias = nomesCategoriasSelecionadas
                .Select((categoria, index) => new SalesAnalysisCategoryLine {
                    CategoriaId = categoria.Id,
                    Nome = categoria.Nome,
                    Cor = coresGrafico[index % coresGrafico.Length],
                    Valores = periodos
                        .Select(periodo => eventosPorCategoriaPeriodo.TryGetValue((categoria.Id, periodo), out var total)
                            ? total
                            : 0)
                        .ToList()
                })
                .ToList();

            var receitaTotal = decimal.Round(pedidosFiltrados.Sum(p => p.Total), 2);

            return new SalesAnalysisResult {
                DataInicio = inicio,
                DataFim = fim,
                Agrupamento = agrupamentoNormalizado,
                TotalPedidos = pedidosFiltrados.Count,
                TotalItensVendidos = itensVendidos.Sum(i => i.Quantidade),
                ReceitaTotal = receitaTotal,
                TicketMedio = pedidosFiltrados.Any() ? decimal.Round(receitaTotal / pedidosFiltrados.Count, 2) : 0,
                QuantidadeProdutosComparados = produtos.Count,
                QuantidadeCategoriasComparadas = categorias.Count,
                EvolucaoPeriodo = evolucaoPeriodo,
                Produtos = produtos,
                Categorias = categorias,
                CategoriasSelecionadas = categoriasSelecionadas,
                CategoriasDisponiveis = categoriasDisponiveis.ToList(),
                GraficoCategorias = graficoCategorias
            };
        }

        public static string NormalizeGrouping(string? agrupamento, DateTime inicio, DateTime fim) {
            var valorInformado = (agrupamento ?? string.Empty).Trim().ToLowerInvariant();
            if (valorInformado is "diario" or "semanal" or "mensal") {
                return valorInformado;
            }

            var intervaloDias = (fim - inicio).TotalDays;
            if (intervaloDias > 180) {
                return "mensal";
            }

            if (intervaloDias > 45) {
                return "semanal";
            }

            return "diario";
        }

        public static DateTime GetPeriodStart(DateTime data, string agrupamento) {
            return agrupamento switch {
                "mensal" => new DateTime(data.Year, data.Month, 1),
                "semanal" => data.Date.AddDays(-((7 + (int)data.DayOfWeek - (int)DayOfWeek.Monday) % 7)),
                _ => data.Date
            };
        }

        public static IEnumerable<DateTime> GeneratePeriods(DateTime inicio, DateTime fim, string agrupamento) {
            var atual = GetPeriodStart(inicio, agrupamento);
            var ultimo = GetPeriodStart(fim, agrupamento);

            while (atual <= ultimo) {
                yield return atual;
                atual = agrupamento switch {
                    "mensal" => atual.AddMonths(1),
                    "semanal" => atual.AddDays(7),
                    _ => atual.AddDays(1)
                };
            }
        }

        public static string FormatPeriodLabel(DateTime data, string agrupamento) {
            return agrupamento switch {
                "mensal" => data.ToString("MM/yyyy"),
                "semanal" => $"{data:dd/MM} - {data.AddDays(6):dd/MM}",
                _ => data.ToString("dd/MM")
            };
        }
    }
}
