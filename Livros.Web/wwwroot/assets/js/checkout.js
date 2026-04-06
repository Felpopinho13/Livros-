const checkoutLayout = document.querySelector('.checkout-layout');

if (checkoutLayout) {
    const quantidadeInput = document.getElementById('quantidade');
    const cupomInput = document.getElementById('cupom');
    const aplicarCupomBtn = document.getElementById('aplicarCupomBtn');
    const enderecoRadios = document.querySelectorAll("input[name='EnderecoId']");
    const metodoSelects = document.querySelectorAll('.payment-method-select');
    const cardSelects = document.querySelectorAll('.saved-card-select');
    const valor1Input = document.querySelector("[name='Valor1']");
    const valor2Input = document.querySelector("[name='Valor2']");

    const unitPrice = parseFloat(checkoutLayout.dataset.unitPrice || '0');
    const freteBase = parseFloat(checkoutLayout.dataset.freteBase || '15');
    const freteExtra = parseFloat(checkoutLayout.dataset.freteExtra || '2');
    const cupomValido = (checkoutLayout.dataset.cupom || '').toUpperCase();

    function formatCurrency(value) {
        return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function parseDecimal(value) {
        if (!value) {
            return 0;
        }

        const normalized = value
            .toString()
            .trim()
            .replace(/\./g, '')
            .replace(',', '.');

        const parsed = parseFloat(normalized);
        return Number.isNaN(parsed) ? 0 : parsed;
    }

    function formatInputValue(value) {
        return formatCurrency(value);
    }

    function getQuantidade() {
        const quantidade = parseInt(quantidadeInput.value, 10);
        return Number.isNaN(quantidade) || quantidade < 1 ? 1 : quantidade;
    }

    function calcularSubtotal() {
        return unitPrice * getQuantidade();
    }

    function calcularFrete() {
        const quantidade = getQuantidade();
        return freteBase + Math.max(quantidade - 1, 0) * freteExtra;
    }

    function calcularDesconto(subtotal) {
        return cupomInput.value.trim().toUpperCase() === cupomValido ? subtotal * 0.10 : 0;
    }

    function obterTotalAtual() {
        return calcularSubtotal() + calcularFrete() - calcularDesconto(calcularSubtotal());
    }

    function atualizarResumo() {
        const subtotal = calcularSubtotal();
        const frete = calcularFrete();
        const desconto = calcularDesconto(subtotal);
        const total = subtotal + frete - desconto;

        document.getElementById('subtotalValor').innerText = `R$ ${formatCurrency(subtotal)}`;
        document.getElementById('freteValor').innerText = `R$ ${formatCurrency(frete)}`;
        document.getElementById('descontoValor').innerText = `R$ ${formatCurrency(desconto)}`;
        document.getElementById('totalCompra').innerText = formatCurrency(total);

        recalcularDivisao(total);
    }

    function recalcularDivisao(total) {
        if (!valor1Input || !valor2Input) {
            return;
        }

        const valor1 = parseDecimal(valor1Input.value);
        const metodo2 = document.querySelector("[name='Metodo2']")?.value;

        if (!metodo2) {
            valor2Input.value = '';
            return;
        }

        const restante = total - valor1;
        valor2Input.value = restante > 0 ? formatInputValue(restante) : formatInputValue(0);
    }

    function toggleNovoEndereco() {
        const novoEnderecoSelecionado = document.querySelector("input[name='EnderecoId']:checked")?.value === '0';
        const novoEnderecoForm = document.getElementById('novoEnderecoForm');

        document.querySelectorAll('.address-card').forEach((card) => {
            const radio = card.querySelector("input[name='EnderecoId']");
            card.classList.toggle('selected', radio?.checked === true);
        });

        if (novoEnderecoForm) {
            novoEnderecoForm.style.display = novoEnderecoSelecionado ? 'block' : 'none';
        }
    }

    function togglePagamento(index) {
        const metodo = document.querySelector(`[name='Metodo${index}']`)?.value;
        const cartaoForm = document.getElementById(`cartaoForm${index}`);

        if (!cartaoForm) {
            return;
        }

        cartaoForm.style.display = metodo === 'cartao' ? 'grid' : 'none';
        if (metodo === 'cartao') {
            toggleNovoCartao(index);
        }
    }

    function toggleNovoCartao(index) {
        const select = document.querySelector(`[name='CartaoId${index}']`);
        const novoCartaoForm = document.getElementById(`novoCartaoForm${index}`);

        if (!novoCartaoForm || !select) {
            return;
        }

        novoCartaoForm.style.display = select.value ? 'none' : 'grid';
    }

    quantidadeInput?.addEventListener('input', atualizarResumo);
    aplicarCupomBtn?.addEventListener('click', atualizarResumo);
    valor1Input?.addEventListener('input', () => recalcularDivisao(obterTotalAtual()));
    valor2Input?.addEventListener('input', () => recalcularDivisao(obterTotalAtual()));

    enderecoRadios.forEach((radio) => radio.addEventListener('change', toggleNovoEndereco));
    metodoSelects.forEach((select) => select.addEventListener('change', () => togglePagamento(select.dataset.index)));
    cardSelects.forEach((select) => select.addEventListener('change', () => toggleNovoCartao(select.dataset.index)));

    toggleNovoEndereco();
    togglePagamento(1);
    togglePagamento(2);
    atualizarResumo();
}
