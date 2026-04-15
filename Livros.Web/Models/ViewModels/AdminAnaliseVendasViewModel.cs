using System;
using System.Collections.Generic;

namespace Livros.Web.Models.ViewModels {
    public class AdminAnaliseVendasViewModel {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int TotalPedidos { get; set; }
        public int TotalItensVendidos { get; set; }
        public decimal ReceitaTotal { get; set; }
        public decimal TicketMedio { get; set; }
        public int QuantidadeProdutosComparados { get; set; }
        public int QuantidadeCategoriasComparadas { get; set; }
        public List<AdminAnalisePeriodoItemViewModel> EvolucaoPeriodo { get; set; } = new();
        public List<AdminAnaliseProdutoItemViewModel> Produtos { get; set; } = new();
        public List<AdminAnaliseCategoriaItemViewModel> Categorias { get; set; } = new();
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
        public string Nome { get; set; } = string.Empty;
        public int UnidadesVendidas { get; set; }
        public int Pedidos { get; set; }
        public decimal Receita { get; set; }
    }
}
