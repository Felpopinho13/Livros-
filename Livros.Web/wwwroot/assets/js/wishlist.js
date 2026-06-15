document.addEventListener("DOMContentLoaded", function () {
    const root = document.getElementById("wishlistRoot");
    if (!root) {
        return;
    }

    const buttons = Array.from(document.querySelectorAll(".btn-wishlist[data-id]"));
    const toggleBtn = document.getElementById("wishlistToggle");
    const modal = document.getElementById("wishlistModal");
    const closeBtn = document.getElementById("closeWishlist");
    const content = document.querySelector(".wishlist-content");
    const count = document.getElementById("wishlistCount");

    if (!toggleBtn || !modal || !closeBtn || !content || !count) {
        return;
    }

    const listUrl = root.dataset.listUrl;
    const addUrl = root.dataset.addUrl;
    const removeUrl = root.dataset.removeUrl;
    const loginUrl = root.dataset.loginUrl || "/Auth/Login";
    const detailsUrlTemplate = root.dataset.detailsUrlTemplate || "/Home/Detalhes/__ID__";

    let wishlistState = {
        isAuthenticated: false,
        count: 0,
        items: []
    };

    function updateCount() {
        count.textContent = String(wishlistState.count || 0);
    }

    function syncButtons() {
        const wishedIds = new Set((wishlistState.items || []).map(function (item) {
            return String(item.livroId);
        }));

        buttons.forEach(function (button) {
            const isActive = wishedIds.has(button.dataset.id);
            button.classList.toggle("active", isActive);
            button.setAttribute("aria-pressed", isActive ? "true" : "false");
        });
    }

    async function fetchWishlist() {
        const response = await fetch(listUrl, {
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        if (!response.ok) {
            throw new Error("Nao foi possivel carregar a lista de desejos.");
        }

        wishlistState = await response.json();
        updateCount();
        syncButtons();
    }

    function buildEmptyState(message, actionElement) {
        const wrapper = document.createElement("div");
        wrapper.className = "wishlist-empty-state";

        const paragraph = document.createElement("p");
        paragraph.textContent = message;
        wrapper.appendChild(paragraph);

        if (actionElement) {
            wrapper.appendChild(actionElement);
        }

        return wrapper;
    }

    function getBookDetailsUrl(bookId) {
        return detailsUrlTemplate.replace("__ID__", String(bookId));
    }

    function renderWishlist() {
        content.innerHTML = "";

        if (!wishlistState.isAuthenticated) {
            const loginLink = document.createElement("a");
            loginLink.href = loginUrl;
            loginLink.className = "btn btn-primary";
            loginLink.textContent = "Entrar";

            content.appendChild(buildEmptyState("Faca login para salvar livros na sua lista de desejos.", loginLink));
            return;
        }

        if (!wishlistState.items || wishlistState.items.length === 0) {
            content.appendChild(buildEmptyState("Sua lista de desejos esta vazia."));
            return;
        }

        wishlistState.items.forEach(function (item) {
            const row = document.createElement("div");
            row.className = "wishlist-item";

            const link = document.createElement("a");
            link.className = "wishlist-item-link";
            link.href = getBookDetailsUrl(item.livroId);

            const image = document.createElement("img");
            image.src = item.imagemUrl;
            image.alt = item.titulo;

            const details = document.createElement("div");
            const title = document.createElement("strong");
            const price = document.createElement("p");

            title.textContent = item.titulo;
            price.textContent = "R$ " + item.preco;

            details.appendChild(title);
            details.appendChild(price);
            link.appendChild(image);
            link.appendChild(details);

            const removeButton = document.createElement("button");
            removeButton.type = "button";
            removeButton.dataset.id = String(item.livroId);
            removeButton.setAttribute("aria-label", "Remover da lista de desejos");
            removeButton.textContent = "x";

            row.appendChild(link);
            row.appendChild(removeButton);

            content.appendChild(row);
        });
    }

    async function sendWishlistRequest(url, bookId) {
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify({ bookId: bookId })
        });

        const data = await response.json();

        if (response.status === 401 || data.requiresAuthentication) {
            wishlistState = {
                isAuthenticated: false,
                count: 0,
                items: []
            };
            updateCount();
            syncButtons();
            renderWishlist();
            modal.classList.add("active");
            return;
        }

        if (!response.ok || !data.succeeded) {
            throw new Error(data.message || "Nao foi possivel atualizar a lista de desejos.");
        }

        await fetchWishlist();
        renderWishlist();
    }

    buttons.forEach(function (button) {
        button.addEventListener("click", async function () {
            const bookId = Number(button.dataset.id);
            if (!bookId) {
                return;
            }

            button.disabled = true;

            try {
                const isActive = button.classList.contains("active");
                await sendWishlistRequest(isActive ? removeUrl : addUrl, bookId);
            } catch (error) {
                alert(error.message || "Nao foi possivel atualizar a lista de desejos.");
            } finally {
                button.disabled = false;
            }
        });
    });

    toggleBtn.addEventListener("click", async function () {
        try {
            await fetchWishlist();
            renderWishlist();
            modal.classList.add("active");
        } catch (error) {
            alert(error.message || "Nao foi possivel abrir a lista de desejos.");
        }
    });

    closeBtn.addEventListener("click", function () {
        modal.classList.remove("active");
    });

    modal.addEventListener("click", function (event) {
        if (event.target === modal) {
            modal.classList.remove("active");
        }
    });

    content.addEventListener("click", async function (event) {
        const button = event.target.closest("button[data-id]");
        if (!button) {
            return;
        }

        const bookId = Number(button.dataset.id);
        if (!bookId) {
            return;
        }

        button.disabled = true;

        try {
            await sendWishlistRequest(removeUrl, bookId);
        } catch (error) {
            alert(error.message || "Nao foi possivel remover o livro da lista de desejos.");
        } finally {
            button.disabled = false;
        }
    });

    fetchWishlist().catch(function () {
        updateCount();
    });
});
