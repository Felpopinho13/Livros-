document.addEventListener("DOMContentLoaded", function () {

    const grid = document.querySelector(".address-grid");
    const modal = document.getElementById("addressModal");
    const openBtn = document.querySelector(".address-header .btn-primary");
    const closeBtn = document.getElementById("closeModal");
    const form = document.getElementById("addressForm");

    // =========================
    // TORNAR PADRÃO
    // =========================
    grid.addEventListener("click", function (e) {

        if (!e.target.classList.contains("make-default")) return;

        e.preventDefault();

        const newCard = e.target.closest(".address-card");
        const currentPrimary = document.querySelector(".address-card.primary");

        if (newCard === currentPrimary) return;

        if (currentPrimary) {
            currentPrimary.classList.remove("primary");

            const oldBadge = currentPrimary.querySelector(".badge");
            if (oldBadge) oldBadge.remove();

            const actions = currentPrimary.querySelector(".address-actions");

            const btn = document.createElement("a");
            btn.href = "#";
            btn.classList.add("make-default");
            btn.innerText = "Tornar padrão";

            actions.appendChild(btn);
        }

        newCard.classList.add("primary");

        let header = newCard.querySelector(".address-top");

        if (!header) {
            const h4 = newCard.querySelector("h4");

            header = document.createElement("div");
            header.classList.add("address-top");

            newCard.insertBefore(header, newCard.firstChild);
            header.appendChild(h4);
        }

        const badge = document.createElement("span");
        badge.classList.add("badge");
        badge.innerText = "Padrão";

        header.appendChild(badge);

        e.target.remove();
    });

    // =========================
    // MODAL
    // =========================
    openBtn.addEventListener("click", function () {
        modal.classList.add("active");
    });

    closeBtn.addEventListener("click", function () {
        modal.classList.remove("active");
    });

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        const nome = form.nome.value;
        const cep = form.cep.value;
        const logradouro = form.logradouro.value;
        const numero = form.numero.value;
        const complemento = form.complemento.value;
        const bairro = form.bairro.value;
        const cidade = form.cidade.value;
        const estado = form.estado.value;

        const card = document.createElement("div");
        card.classList.add("address-card");

        card.innerHTML = `
            <h4>${nome}</h4>
            <p>${logradouro}, ${numero}${complemento ? " - " + complemento : ""}</p>
            <p>${bairro}</p>
            <p>${cidade} - ${estado}</p>
            <p>CEP: ${cep}</p>

            <div class="address-actions">
                <a href="#">Excluir</a>
                <a href="editar-endereco.html">Editar</a>
                <a href="#" class="make-default">Tornar padrão</a>
            </div>
        `;

        grid.appendChild(card);

        modal.classList.remove("active");
        form.reset();
    });

});