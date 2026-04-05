let subtotal = 0;
let frete = 0;

document.addEventListener("DOMContentLoaded", () => {
    subtotal = parseFloat(document.getElementById("totalCompra").innerText.replace(",", "."));

    document.querySelector("[name='Valor1']").addEventListener("input", calcularDivisao);
    document.querySelector("[name='Valor2']").addEventListener("input", calcularDivisao);
});

// 🚚 FRETE
function calcularFrete() {
    frete = 15.00;

    document.getElementById("freteValor").innerText = "R$ " + frete.toFixed(2);

    atualizarTotal();
}

// 🎟 CUPOM
function aplicarCupom() {
    let cupom = document.getElementById("cupom").value;

    if (cupom === "DESCONTO10") {
        subtotal = subtotal * 0.9;
        atualizarTotal();
        alert("Cupom aplicado!");
    } else {
        alert("Cupom inválido");
    }
}

// 💰 TOTAL
function atualizarTotal() {
    let total = subtotal + frete;

    document.getElementById("totalCompra").innerText = total.toFixed(2).replace(".", ",");

    calcularDivisao();
}

// 💳 DIVISÃO DE PAGAMENTO
function calcularDivisao() {
    let valor1 = parseFloat(document.querySelector("[name='Valor1']").value) || 0;
    let valor2Input = document.querySelector("[name='Valor2']");

    let total = subtotal + frete;

    if (valor1 > total) {
        valor1 = total;
    }

    let restante = total - valor1;

    valor2Input.value = restante > 0 ? restante.toFixed(2) : "";
}

function selecionarEndereco(id, elemento) {
    document.querySelectorAll(".address-card").forEach(e => {
        e.classList.remove("selected");
    });

    elemento.classList.add("selected");

    elemento.querySelector("input[type='radio']").checked = true;

    document.getElementById("novoEnderecoForm").style.display = "none";
}

function selecionarNovoEndereco(elemento) {
    document.querySelectorAll(".address-card").forEach(e => {
        e.classList.remove("selected");
    });

    elemento.classList.add("selected");

    elemento.querySelector("input[type='radio']").checked = true;

    document.getElementById("novoEnderecoForm").style.display = "block";
}

function togglePagamento(tipo, index) {
    const form = document.getElementById("cartaoForm" + index);

    if (!form) return;

    if (tipo === "cartao") {
        form.style.display = "grid"; // 🔥 usa grid agora
    } else {
        form.style.display = "none";
    }
}

function validarPagamento() {
    let v1 = parseFloat(document.querySelector("[name='Valor1']").value) || 0;
    let v2 = parseFloat(document.querySelector("[name='Valor2']").value) || 0;

    let total = parseFloat(document.getElementById("totalCompra").innerText.replace(",", "."));

    if ((v1 + v2) !== total) {
        alert("A soma dos pagamentos deve ser igual ao total!");
        return false;
    }

    return true;
}