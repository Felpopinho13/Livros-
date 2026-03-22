let cartCount = 0;

const cartDisplay = document.getElementById("cart-count");

if (cartDisplay) {

    const buttons = document.querySelectorAll(".btn-primary");

    buttons.forEach(button => {
        button.addEventListener("click", () => {
            cartCount++;
            cartDisplay.innerText = cartCount;
        });
    });

}