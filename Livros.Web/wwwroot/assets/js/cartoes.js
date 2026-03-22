// 🔥 MODAL
const modal = document.getElementById("cardModal");
const openBtn = document.getElementById("openCardModal");
const closeBtn = document.getElementById("closeCardModal");

if (openBtn && closeBtn && modal) {
    openBtn.onclick = () => modal.style.display = "flex";
    closeBtn.onclick = () => modal.style.display = "none";
}

// 🔥 NÚMERO DO CARTÃO
const numeroInput = document.getElementById("numeroCartao");

if (numeroInput) {
    numeroInput.addEventListener("input", function (e) {
        let valor = e.target.value.replace(/\D/g, "");

        valor = valor.substring(0, 16);

        valor = valor.replace(/(\d{4})(?=\d)/g, "$1 ");

        e.target.value = valor;
    });
}

// 🔥 VALIDADE (MM/AA)
const validadeInput = document.getElementById("validadeCartao");

if (validadeInput) {
    validadeInput.addEventListener("input", function (e) {
        let valor = e.target.value.replace(/\D/g, "");

        valor = valor.substring(0, 4);

        // valida mês
        if (valor.length >= 2) {
            let mes = parseInt(valor.substring(0, 2));

            if (mes < 1 || mes > 12) {
                valor = "";
            }
        }

        if (valor.length >= 3) {
            valor = valor.replace(/(\d{2})(\d{1,2})/, "$1/$2");
        }

        e.target.value = valor;
    });
}

// 🔥 CVV
const cvvInput = document.getElementById("cvvCartao");

if (cvvInput) {
    cvvInput.addEventListener("input", function (e) {
        let valor = e.target.value.replace(/\D/g, "");
        e.target.value = valor.substring(0, 3);
    });
}