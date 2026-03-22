// 🔥 CPF (000.000.000-00)
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

// 🔥 TELEFONE ((11) 99999-9999)
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

// 🔥 CEP (00000-000)
const cepInput = document.getElementById("cep");

if (cepInput) {
    cepInput.addEventListener("input", function (e) {
        let v = e.target.value.replace(/\D/g, "").substring(0, 8);

        v = v.replace(/(\d{5})(\d)/, "$1-$2");

        e.target.value = v;
    });
}

// 🔥 SENHA (mínimo 6 caracteres)
const senhaInput = document.querySelector('input[name="senha"]');

if (senhaInput) {
    senhaInput.addEventListener("blur", function () {
        if (senhaInput.value.length < 6) {
            alert("A senha deve ter pelo menos 6 caracteres.");
            senhaInput.focus();
        }
    });
}