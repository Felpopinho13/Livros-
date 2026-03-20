document.addEventListener("DOMContentLoaded", function () {

    const usuario = localStorage.getItem("usuarioLogado");
    if (!usuario) return;

    const storageKey = "wishlist_" + usuario;

    const buttons = document.querySelectorAll(".btn-wishlist");
    const toggleBtn = document.getElementById("wishlistToggle");
    const modal = document.getElementById("wishlistModal");
    const closeBtn = document.getElementById("closeWishlist");
    const content = document.querySelector(".wishlist-content");
    const count = document.getElementById("wishlistCount");

    function getWishlist() {
        return JSON.parse(localStorage.getItem(storageKey)) || [];
    }

    function saveWishlist(data) {
        localStorage.setItem(storageKey, JSON.stringify(data));
        updateCount();
    }

    function updateCount() {
        count.innerText = getWishlist().length;
    }

    function renderWishlist() {
        const wishlist = getWishlist();
        content.innerHTML = "";

        if (wishlist.length === 0) {
            content.innerHTML = "<p>Sua lista está vazia.</p>";
            return;
        }

        wishlist.forEach(item => {

            const div = document.createElement("div");
            div.classList.add("wishlist-item");

            div.innerHTML = `
                <img src="${item.imagem}">
                <div>
                  <strong>${item.titulo}</strong>
                  <p>R$ ${item.preco}</p>
                </div>
                <button data-id="${item.id}">✕</button>
            `;

            content.appendChild(div);
        });
    }

    // Atualizar contador ao carregar
    updateCount();

    // Botão coração nos cards
    buttons.forEach(button => {

        const id = button.dataset.id;
        const wishlist = getWishlist();

        if (wishlist.find(item => item.id === id)) {
            button.classList.add("active");
        }

        button.addEventListener("click", function () {

            let wishlist = getWishlist();
            const exists = wishlist.find(item => item.id === id);

            if (exists) {
                wishlist = wishlist.filter(item => item.id !== id);
                button.classList.remove("active");
            } else {
                wishlist.push({
                    id: button.dataset.id,
                    titulo: button.dataset.titulo,
                    preco: button.dataset.preco,
                    imagem: button.dataset.imagem
                });
                button.classList.add("active");
            }

            saveWishlist(wishlist);
        });

    });

    // Abrir modal
    toggleBtn.addEventListener("click", function () {
        renderWishlist();
        modal.classList.add("active");
    });

    // Fechar modal
    closeBtn.addEventListener("click", function () {
        modal.classList.remove("active");
    });

    // Remover item dentro do modal
    content.addEventListener("click", function (e) {
        if (e.target.tagName !== "BUTTON") return;

        const id = e.target.dataset.id;
        let wishlist = getWishlist();
        wishlist = wishlist.filter(item => item.id !== id);

        saveWishlist(wishlist);
        renderWishlist();

        // Atualizar botão coração no card
        const btn = document.querySelector(`.btn-wishlist[data-id="${id}"]`);
        if (btn) btn.classList.remove("active");
    });

});