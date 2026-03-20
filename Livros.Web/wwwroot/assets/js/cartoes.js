document.addEventListener("DOMContentLoaded", function () {

    const grid = document.querySelector(".wallet-grid");
    const modal = document.getElementById("cardModal");
    const openBtn = document.querySelector(".wallet-header .btn-primary");
    const closeBtn = document.getElementById("closeCardModal");
    const form = document.getElementById("cardForm");

    // =========================
    // ABRIR / FECHAR MODAL
    // =========================
    openBtn.addEventListener("click", function () {
        modal.classList.add("active");
    });

    closeBtn.addEventListener("click", function () {
        modal.classList.remove("active");
    });

    // =========================
    // MÁSCARA NÚMERO CARTÃO
    // =========================
    form.numero.addEventListener("input", function (e) {
        let value = e.target.value.replace(/\D/g, "");
        value = value.replace(/(\d{4})/g, "$1 ").trim();
        e.target.value = value;
    });

    // =========================
    // CADASTRAR CARTÃO
    // =========================
    form.addEventListener("submit", function (e) {
        e.preventDefault();

        const nome = form.nome.value;
        const numero = form.numero.value;
        const validade = form.validade.value;

        const finalDigits = numero.replace(/\s/g, "").slice(-4);

        const card = document.createElement("div");
        card.classList.add("wallet-card");

        card.innerHTML = `
            <div class="wallet-card-top">
                <strong>${nome}</strong>
            </div>

            <p>**** **** **** ${finalDigits}</p>
            <p>Vence em ${validade}</p>

            <div class="wallet-actions">
                <a href="#">Remover</a>
                <a href="#" class="make-default">Tornar padrão</a>
            </div>
        `;

        grid.appendChild(card);

        // Se for o primeiro cartão, já torna padrão
        if (!document.querySelector(".wallet-card.primary")) {
            card.classList.add("primary");

            const header = card.querySelector(".wallet-card-top");
            const badge = document.createElement("span");
            badge.classList.add("badge");
            badge.innerText = "Padrão";
            header.appendChild(badge);

            const btn = card.querySelector(".make-default");
            if (btn) btn.remove();
        }

        modal.classList.remove("active");
        form.reset();
    });

    // =========================
    // TORNAR PADRÃO
    // =========================
    grid.addEventListener("click", function (e) {

        if (!e.target.classList.contains("make-default")) return;

        e.preventDefault();

        const newCard = e.target.closest(".wallet-card");
        const currentPrimary = document.querySelector(".wallet-card.primary");

        if (newCard === currentPrimary) return;

        if (currentPrimary) {
            currentPrimary.classList.remove("primary");

            const oldBadge = currentPrimary.querySelector(".badge");
            if (oldBadge) oldBadge.remove();

            const actions = currentPrimary.querySelector(".wallet-actions");

            const btn = document.createElement("a");
            btn.href = "#";
            btn.classList.add("make-default");
            btn.innerText = "Tornar padrão";

            actions.appendChild(btn);
        }

        newCard.classList.add("primary");

        const header = newCard.querySelector(".wallet-card-top");

        const badge = document.createElement("span");
        badge.classList.add("badge");
        badge.innerText = "Padrão";

        header.appendChild(badge);

        e.target.remove();
    });

});