using System;
using System.Collections.Generic;

namespace Livros.Web.Models.ViewModels {
    public class MeusCuponsViewModel {
        public string NomeCliente { get; set; } = string.Empty;
        public int TotalCupons { get; set; }
        public int CuponsDisponiveis { get; set; }
        public decimal ValorDisponivel { get; set; }
        public List<MeuCupomItemViewModel> Cupons { get; set; } = new();
    }

    public class MeuCupomItemViewModel {
        public string Codigo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataUtilizacao { get; set; }
        public int? PedidoId { get; set; }
    }
}
