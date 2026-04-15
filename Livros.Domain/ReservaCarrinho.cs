namespace Livros.Domain {
    public class ReservaCarrinho {
        public int Id { get; set; }

        public int LivroId { get; set; }
        public Livro? Livro { get; set; }

        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public string? SessionKey { get; set; }

        public int Quantidade { get; set; }
        public DateTime ReservadoEm { get; set; }
        public DateTime ExpiraEm { get; set; }
    }
}
