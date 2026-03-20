document.addEventListener("DOMContentLoaded", function () {

    const usuario = localStorage.getItem("usuarioLogado");
    if (!usuario) return;

    const modal = document.getElementById("reviewModal");
    const title = document.getElementById("reviewTitle");
    const rating = document.getElementById("reviewRating");
    const comment = document.getElementById("reviewComment");
    const submit = document.getElementById("submitReview");
    const close = document.getElementById("closeReview");

    let livroAtual = null;

    document.querySelectorAll(".btn-avaliar").forEach(btn => {
        btn.addEventListener("click", function () {
            livroAtual = this.dataset.id;
            title.innerText = "Avaliar: " + this.dataset.titulo;
            modal.classList.add("active");
        });
    });

    close.addEventListener("click", function () {
        modal.classList.remove("active");
    });

    submit.addEventListener("click", function () {

        const avaliacoes = JSON.parse(localStorage.getItem("avaliacoes")) || [];

        // impedir avaliação duplicada
        const jaAvaliado = avaliacoes.find(a =>
            a.usuario === usuario && a.livroId === livroAtual
        );

        if (jaAvaliado) {
            alert("Você já avaliou este livro.");
            return;
        }

        avaliacoes.push({
            usuario: usuario,
            livroId: livroAtual,
            nota: rating.value,
            comentario: comment.value
        });

        localStorage.setItem("avaliacoes", JSON.stringify(avaliacoes));

        alert("Avaliação enviada com sucesso!");
        modal.classList.remove("active");
    });

});