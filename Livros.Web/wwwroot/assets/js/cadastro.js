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

const form = document.querySelector(".register-form");

if (form) {
    form.addEventListener("submit", function (e) {
        const senha = document.querySelector('input[name="senha"]')?.value ?? "";
        const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*[^A-Za-z0-9\s])(?=\S+$).{8,}$/;

        if (!regex.test(senha)) {
            e.preventDefault();
            alert("A senha deve ter pelo menos 8 caracteres, com letra maiuscula, minuscula, caractere especial e sem espacos.");
        }
    });
}
