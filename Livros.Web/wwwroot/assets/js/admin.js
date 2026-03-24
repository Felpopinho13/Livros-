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
    document.getElementById("editCpf").value = cpf;
    document.getElementById("editTelefone").value = telefone;
    document.getElementById("editGenero").value = genero;
    document.getElementById("editDataNascimento").value = dataNascimento;

    document.getElementById("editIsAdmin").value = isAdmin.toString();

    document.getElementById("editarClienteModal").style.display = "flex";
}

function fecharModalEditar() {
    document.getElementById("editarClienteModal").style.display = "none";
}