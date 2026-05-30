(function () {
    const root = document.querySelector("[data-chatbot-root]");
    if (!root) {
        return;
    }

    const endpoint = root.getAttribute("data-endpoint");
    const toggle = root.querySelector("[data-chatbot-toggle]");
    const panel = root.querySelector("[data-chatbot-panel]");
    const form = root.querySelector("[data-chatbot-form]");
    const input = root.querySelector("[data-chatbot-input]");
    const submit = root.querySelector("[data-chatbot-submit]");
    const messages = root.querySelector("[data-chatbot-messages]");

    function appendMessage(role, text, recommendations) {
        const wrapper = document.createElement("div");
        wrapper.className = `chatbot-message ${role}`;

        const bubble = document.createElement("div");
        bubble.className = "chatbot-message-bubble";
        bubble.textContent = text;
        wrapper.appendChild(bubble);

        if (role === "assistant" && Array.isArray(recommendations) && recommendations.length > 0) {
            const cards = document.createElement("div");
            cards.className = "chatbot-recommendations";

            recommendations.forEach((book) => {
                const card = document.createElement("a");
                card.className = "chatbot-book-card";
                card.href = book.detailsUrl;

                const image = document.createElement("img");
                image.src = book.imageUrl || "";
                image.alt = book.title || "Livro";
                card.appendChild(image);

                const body = document.createElement("div");
                body.className = "chatbot-book-body";

                const title = document.createElement("strong");
                title.textContent = book.title;
                body.appendChild(title);

                const author = document.createElement("span");
                author.textContent = book.author;
                body.appendChild(author);

                const price = document.createElement("small");
                price.textContent = book.price;
                body.appendChild(price);

                if (Array.isArray(book.categories) && book.categories.length > 0) {
                    const categories = document.createElement("span");
                    categories.className = "chatbot-book-categories";
                    categories.textContent = book.categories.join(" | ");
                    body.appendChild(categories);
                }

                const reason = document.createElement("span");
                reason.className = "chatbot-book-reason";
                reason.textContent = book.reason;
                body.appendChild(reason);

                card.appendChild(body);
                cards.appendChild(card);
            });

            wrapper.appendChild(cards);
        }

        messages.appendChild(wrapper);
        messages.scrollTop = messages.scrollHeight;
    }

    async function sendMessage(message) {
        appendMessage("user", message);

        submit.disabled = true;
        input.disabled = true;
        appendMessage("assistant", "Estou analisando o catalogo para montar a melhor sugestao.");

        try {
            const response = await fetch(endpoint, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ message })
            });

            const payload = await response.json();
            messages.removeChild(messages.lastElementChild);

            if (!response.ok) {
                appendMessage("assistant", payload.error || "Nao consegui responder agora. Tente novamente em instantes.");
                return;
            }

            appendMessage("assistant", payload.reply, payload.recommendations || []);
        } catch (error) {
            messages.removeChild(messages.lastElementChild);
            appendMessage("assistant", "Nao consegui acessar o assistente agora. Tente novamente em instantes.");
        } finally {
            submit.disabled = false;
            input.disabled = false;
            input.focus();
        }
    }

    toggle.addEventListener("click", function () {
        root.classList.toggle("is-open");
        panel.setAttribute("aria-hidden", root.classList.contains("is-open") ? "false" : "true");

        if (root.classList.contains("is-open")) {
            input.focus();
        }
    });

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        const message = input.value.trim();

        if (!message) {
            input.focus();
            return;
        }

        input.value = "";
        sendMessage(message);
    });

    input.addEventListener("keydown", function (event) {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            form.requestSubmit();
        }
    });
})();
