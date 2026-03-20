document.addEventListener("DOMContentLoaded", function () {

    const radios = document.querySelectorAll("input[name='payment']");
    const forms = document.querySelectorAll(".payment-form");

    const finalizarBtn = document.getElementById("finalizarPagamento");

    const modal = document.getElementById("paymentModal");
    const message = document.getElementById("paymentMessage");
    const closeModal = document.getElementById("closePaymentModal");

    // =============================
    // Mostrar formulário correto
    // =============================
    radios.forEach(radio => {
        radio.addEventListener("change", function () {

            forms.forEach(form => form.style.display = "none");

            if (this.value === "pix") {
                document.getElementById("pixForm").style.display = "flex";
            }

            if (this.value === "boleto") {
                document.getElementById("boletoForm").style.display = "flex";
            }

            if (this.value === "cartao") {
                document.getElementById("cartaoForm").style.display = "grid";
            }
        });
    });

    // =============================
    // Finalizar pagamento
    // =============================
    finalizarBtn.addEventListener("click", function () {

        const selected = document.querySelector("input[name='payment']:checked");

        if (!selected) {
            alert("Selecione uma forma de pagamento.");
            return;
        }

        if (selected.value === "pix") {
            message.innerText = "Finalizando pagamento via Pix...";
        }

        if (selected.value === "boleto") {
            message.innerText = "Gerando boleto...";
        }

        if (selected.value === "cartao") {
            message.innerText = "Processando pagamento...";
        }

        modal.classList.add("active");
    });

    closeModal.addEventListener("click", function () {
        modal.classList.remove("active");

        // Simulação de redirecionamento após fechar
        window.location.href = "pedido-confirmado.html";
    });

});

const addressCards = document.querySelectorAll(".address-card");

addressCards.forEach(card => {

  card.addEventListener("click", () => {

    addressCards.forEach(c => c.classList.remove("selected"));
    card.classList.add("selected");

  });

});

const creditCards = document.querySelectorAll(".credit-card");

creditCards.forEach(card => {

  card.addEventListener("click", () => {

    creditCards.forEach(c => c.classList.remove("selected"));
    card.classList.add("selected");

  });

});