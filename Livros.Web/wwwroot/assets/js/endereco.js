// MODAL
const modal = document.getElementById("addressModal");
const openBtn = document.getElementById("openModal");
const closeBtn = document.getElementById("closeModal");

if (openBtn) openBtn.onclick = () => modal.style.display = "flex";
if (closeBtn) closeBtn.onclick = () => modal.style.display = "none";

window.onclick = (e) => {
    if (e.target == modal) modal.style.display = "none";
};

// 🔥 CEP (00000-000)
const cepInput = document.querySelector("input[name='cep']");

if (cepInput) {
    cepInput.addEventListener("input", function (e) {
        let valor = e.target.value.replace(/\D/g, "");
        valor = valor.substring(0, 8);

        if (valor.length > 5) {
            valor = valor.replace(/(\d{5})(\d+)/, "$1-$2");
        }

        e.target.value = valor;
    });
}

// 🔥 ESTADO (SP)
const estadoInput = document.querySelector("input[name='estado']");

if (estadoInput) {
    estadoInput.addEventListener("input", function (e) {
        let valor = e.target.value.replace(/[^a-zA-Z]/g, "");
        e.target.value = valor.toUpperCase().substring(0, 2);
    });
}