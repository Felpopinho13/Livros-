setTimeout(() => {
    const alert = document.querySelector('.alert-success');
    if (alert) alert.style.display = 'none';
}, 3000);

// MODAL SENHA
const senhaModal = document.getElementById("senhaModal");
const openSenhaBtn = document.getElementById("openSenhaModal");
const closeSenhaBtn = document.getElementById("closeSenhaModal");

if (openSenhaBtn) {
    openSenhaBtn.onclick = () => senhaModal.style.display = "flex";
}

if (closeSenhaBtn) {
    closeSenhaBtn.onclick = () => senhaModal.style.display = "none";
}

function validarSenha() {
    const senha = document.getElementById("novaSenha").value;
    const confirmar = document.getElementById("confirmarSenha").value;

    const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*[\W_]).{8,}$/;

    if (!regex.test(senha)) {
        alert("A senha deve ter no mínimo 8 caracteres, letras maiúsculas, minúsculas e símbolo.");
        return false;
    }

    if (senha !== confirmar) {
        alert("As senhas não coincidem!");
        return false;
    }

    return true;
}

// FECHAR MODAL AO CLICAR FORA
window.onclick = (e) => {
    if (e.target == senhaModal) {
        senhaModal.style.display = "none";
    }
};