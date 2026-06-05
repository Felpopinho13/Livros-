using System;
using System.Collections.Generic;

namespace Livros.Web.Models.ViewModels {
    public class AdminAnaliseVendasViewModel {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Agrupamento { get; set; } = "mensal";
        public int TotalPedidos { get; set; }
        public int TotalItensVendidos { get; set; }
        public decimal ReceitaTotal { get; set; }
        public decimal TicketMedio { get; set; }
        public int QuantidadeProdutosComparados { get; set; }
        public int QuantidadeCategoriasComparadas { get; set; }
        public List<AdminAnalisePeriodoItemViewModel> EvolucaoPeriodo { get; set; } = new();
        public List<AdminAnaliseProdutoItemViewModel> Produtos { get; set; } = new();
        public List<AdminAnaliseCategoriaItemViewModel> Categorias { get; set; } = new();
        public List<int> CategoriasSelecionadas { get; set; } = new();
        public List<AdminAnaliseCategoriaOptionViewModel> CategoriasDisponiveis { get; set; } = new();
        public List<AdminAnaliseCategoriaLinhaViewModel> GraficoCategorias { get; set; } = new();
    }

    public class AdminAnalisePeriodoItemViewModel {
        public string Rotulo { get; set; } = string.Empty;
        public decimal Receita { get; set; }
        public int Pedidos { get; set; }
    }

    public class AdminAnaliseProdutoItemViewModel {
        public string Titulo { get; set; } = string.Empty;
        public int UnidadesVendidas { get; set; }
        public int Pedidos { get; set; }
        public decimal Receita { get; set; }
    }

    public class AdminAnaliseCategoriaItemViewModel {
        public int CategoriaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int UnidadesVendidas { get; set; }
        public int Pedidos { get; set; }
        public decimal Receita { get; set; }
    }

    public class AdminAnaliseCategoriaOptionViewModel {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class AdminAnaliseCategoriaLinhaViewModel {
        public int CategoriaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cor { get; set; } = string.Empty;
        public List<int> Valores { get; set; } = new();
    }
}
