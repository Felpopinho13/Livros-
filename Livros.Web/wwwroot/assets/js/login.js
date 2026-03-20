document.addEventListener("DOMContentLoaded", function () {

    const usuario = localStorage.getItem("usuarioLogado");
    const paginaAtual = window.location.pathname.split("/").pop();

    const paginasProtegidas = [
        "area-cliente.html",
        "enderecos-cliente.html",
        "meus-pedidos.html",
        "editar-cliente.html"
    ];

    // =========================
    // REDIRECIONAR SE JÁ LOGADO
    // =========================
    if (usuario && paginaAtual === "login.html") {
        window.location.href = "area-cliente.html";
        return;
    }

    // =========================
    // PROTEGER PÁGINAS PRIVADAS
    // =========================
    if (paginasProtegidas.includes(paginaAtual) && !usuario) {
        window.location.href = "login.html";
        return;
    }

    // =========================
    // LOGIN
    // =========================
    const form = document.getElementById("login-form");

    if (form) {
        form.addEventListener("submit", function (event) {
            event.preventDefault();

            const email = form.querySelector("input[type='email']").value.trim();
            const senha = form.querySelector("input[type='password']").value.trim();

            if (!email || !senha) {
                alert("Preencha todos os campos.");
                return;
            }

            // Simulação de autenticação
            localStorage.setItem("usuarioLogado", email);

            window.location.href = "area-cliente.html";
        });
    }

    // =========================
    // LOGOUT
    // =========================
    const logoutBtn = document.getElementById("logoutBtn");

    if (logoutBtn) {
        logoutBtn.addEventListener("click", function () {
            localStorage.removeItem("usuarioLogado");
            window.location.href = "index.html";
        });
    }

});