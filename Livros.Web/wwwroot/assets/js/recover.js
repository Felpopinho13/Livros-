document.addEventListener("DOMContentLoaded", function () {

    const form = document.getElementById("recoverForm");
    const box = document.getElementById("recoverBox");

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        const email = document.getElementById("recoverEmail").value;

        // Simulação de envio
        setTimeout(() => {

            box.innerHTML = `
                <h2>Email enviado 📩</h2>
                <p class="auth-success">
                    Enviamos um link de recuperação para <strong>${email}</strong>.
                </p>
                <p>
                    Acesse seu e-mail para continuar com a redefinição de senha.
                </p>
                <a href="login.html" class="btn-primary btn-block">
                    Voltar para login
                </a>
            `;

        }, 800);

    });

});