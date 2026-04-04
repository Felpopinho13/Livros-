const cpfInput = document.querySelector('input[name="cpf"]');

if (cpfInput) {
    cpfInput.addEventListener("input", function (e) {
        let v = e.target.value.replace(/\D/g, "").substring(0, 11);

        v = v.replace(/(\d{3})(\d)/, "$1.$2");
        v = v.replace(/(\d{3})(\d)/, "$1.$2");
        v = v.replace(/(\d{3})(\d{1,2})$/, "$1-$2");

        e.target.value = v;
    });
}

const telInput = document.querySelector('input[name="telefone"]');

if (telInput) {
    telInput.addEventListener("input", function (e) {
        let v = e.target.value.replace(/\D/g, "").substring(0, 11);

        if (v.length > 10) {
            v = v.replace(/^(\d{2})(\d{5})(\d{4})$/, "($1) $2-$3");
        } else {
            v = v.replace(/^(\d{2})(\d{4})(\d{0,4})$/, "($1) $2-$3");
        }

        e.target.value = v;
    });
}

const cepInput = document.getElementById("cep");

if (cepInput) {
    cepInput.addEventListener("input", function (e) {
        let v = e.target.value.replace(/\D/g, "").substring(0, 8);

        v = v.replace(/(\d{5})(\d)/, "$1-$2");

        e.target.value = v;
    });
}

const senhaInput = document.querySelector('input[name="senha"]');

if (senhaInput) {
    senhaInput.addEventListener("blur", function () {
        if (senhaInput.value.length < 6) {
            alert("A senha deve ter pelo menos 6 caracteres.");
        }
    });
}

const form = document.querySelector(".register-form");

form.addEventListener("submit", function (e) {
    const senha = document.querySelector('input[name="senha"]').value;

    const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*[\W_]).{8,}$/;

    if (!regex.test(senha)) {
        e.preventDefault();
        alert("A senha deve ter no mínimo 8 caracteres, com letra maiúscula, minúscula e símbolo.");
    }
});