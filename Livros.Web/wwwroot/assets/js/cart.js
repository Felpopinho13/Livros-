const cartDisplay = document.getElementById("cart-count");

if (cartDisplay) {
    const currentValue = parseInt(cartDisplay.textContent || "0", 10);
    cartDisplay.textContent = Number.isNaN(currentValue) ? "0" : currentValue.toString();
}
