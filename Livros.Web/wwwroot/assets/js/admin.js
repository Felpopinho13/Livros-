const modal = document.getElementById("clienteModal");
const openBtn = document.getElementById("openClienteModal");
const closeBtn = document.getElementById("closeClienteModal");
const cancelBtn = document.getElementById("cancelClienteModal");

if (openBtn) {
    openBtn.onclick = () => modal.style.display = "flex";
}

if (closeBtn) {
    closeBtn.onclick = () => modal.style.display = "none";
}

if (cancelBtn) {
    cancelBtn.onclick = () => modal.style.display = "none";
}

window.onclick = (e) => {
    if (e.target == modal) {
        modal.style.display = "none";
    }
};


document.addEventListener("input", function (e) {
    if (e.target.name === "CPF") {

        let v = e.target.value.replace(/\D/g, "");

        v = v.replace(/^(\d{3})(\d)/, "$1.$2");
        v = v.replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3");
        v = v.replace(/\.(\d{3})(\d)/, ".$1-$2");

        e.target.value = v;
    }
});

function abrirModalVer(nome, email, cpf, telefone, genero, dataNascimento, status, tipo) {
    document.getElementById("verNome").value = nome;
    document.getElementById("verEmail").value = email;
    document.getElementById("verCpf").value = cpf;
    document.getElementById("verTelefone").value = telefone;
    document.getElementById("verGenero").value = genero;
    document.getElementById("verDataNascimento").value = dataNascimento;
    document.getElementById("verStatus").value = status;
    document.getElementById("verTipo").value = tipo;

    document.getElementById("verClienteModal").style.display = "flex";
}

function fecharModalVer() {
    document.getElementById("verClienteModal").style.display = "none";
}

function abrirModalEditar(id, nome, email, cpf, telefone, genero, dataNascimento, isAdmin) {

    document.getElementById("editId").value = id;
    document.getElementById("editNome").value = nome;
    document.getElementById("editEmail").value = email;

    let cpfLimpo = cpf ? cpf.replace(/\D/g, "").substring(0, 11) : "";
    document.getElementById("editCpf").value = cpfLimpo;

    document.getElementById("editTelefone").value = telefone;
    document.getElementById("editGenero").value = genero;
    document.getElementById("editDataNascimento").value = dataNascimento;

    document.getElementById("editIsAdmin").value = isAdmin.toString();

    document.getElementById("editarClienteModal").style.display = "flex";
}

document.addEventListener("input", function (e) {
    if (e.target.name === "Telefone") {

        let v = e.target.value.replace(/\D/g, "").substring(0, 11);

        if (v.length <= 10) {
            v = v.replace(/^(\d{2})(\d)/, "($1) $2");
            v = v.replace(/(\d{4})(\d)/, "$1-$2");
        } else {
            v = v.replace(/^(\d{2})(\d)/, "($1) $2");
            v = v.replace(/(\d{5})(\d)/, "$1-$2");
        }

        e.target.value = v;
    }
});

function fecharModalEditar() {
    document.getElementById("editarClienteModal").style.display = "none";
}
