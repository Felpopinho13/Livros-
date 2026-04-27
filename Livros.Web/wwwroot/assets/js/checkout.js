const checkoutLayout = document.querySelector('.checkout-layout');

if (checkoutLayout) {
    const quantidadeInput = document.getElementById('quantidade');
    const cupomInput = document.getElementById('cupom');
    const aplicarCupomBtn = document.getElementById('aplicarCupomBtn');
    const cupomCheckboxes = document.querySelectorAll("input[name='CuponsTrocaSelecionados']");
    const cupomMensagem = document.getElementById('cupomMensagem');
    const enderecoRadios = document.querySelectorAll("input[name='EnderecoId']");
    const metodoSelects = document.querySelectorAll('.payment-method-select');
    const cardSelects = document.querySelectorAll('.saved-card-select');
    const valor1Input = document.querySelector("[name='Valor1']");
    const valor2Input = document.querySelector("[name='Valor2']");
    const toggleSegundoPagamentoBtn = document.getElementById('toggleSegundoPagamento');
    const segundoPagamentoWrapper = document.getElementById('segundoPagamentoWrapper');
    const metodo2Select = document.querySelector("[name='Metodo2']");
    const estadoNovoEnderecoInput = document.getElementById('estadoNovoEndereco');
    const tipoEntregaSelect = document.getElementById('tipoEntrega');
    const dataEntregaProgramadaWrapper = document.getElementById('dataEntregaProgramadaWrapper');
    const dataEntregaPrevistaInput = document.getElementById('dataEntregaPrevista');

    const unitPrice = parseFloat(checkoutLayout.dataset.unitPrice || '0');
    const subtotalBase = parseFloat(checkoutLayout.dataset.subtotalBase || '0');
    const quantidadeBase = parseInt(checkoutLayout.dataset.quantidadeBase || '1', 10);
    const permiteQuantidade = checkoutLayout.dataset.permiteQuantidade === 'true';
    const freightUrl = checkoutLayout.dataset.freteUrl || '';

    let descontoAplicado = parseDecimal(document.getElementById('descontoValor')?.textContent || '0');
    let cupomAplicadoCodigo = cupomInput?.value?.trim() || '';
    let segundoPagamentoAtivo = segundoPagamentoWrapper?.style.display !== 'none';
    let freteAtual = parseFloat(checkoutLayout.dataset.frete || '0');

    function formatCurrency(value) {
        return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function parseDecimal(value) {
        if (!value) {
            return 0;
        }

        const normalized = value
            .toString()
            .replace(/R\$/g, '')
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
        if (!permiteQuantidade || !quantidadeInput) {
            return Number.isNaN(quantidadeBase) || quantidadeBase < 1 ? 1 : quantidadeBase;
        }

        const quantidade = parseInt(quantidadeInput.value, 10);
        return Number.isNaN(quantidade) || quantidade < 1 ? 1 : quantidade;
    }

    function calcularSubtotal() {
        if (!permiteQuantidade) {
            return subtotalBase;
        }

        return unitPrice * getQuantidade();
    }

    function obterTotalAtual() {
        return Math.max(calcularSubtotal() + freteAtual - descontoAplicado, 0);
    }

    function atualizarResumo() {
        const subtotal = calcularSubtotal();
        const baseDesconto = subtotal + freteAtual;
        const desconto = Math.min(baseDesconto, descontoAplicado);
        const total = Math.max(baseDesconto - desconto, 0);

        document.getElementById('subtotalValor').innerText = `R$ ${formatCurrency(subtotal)}`;
        document.getElementById('freteValor').innerText = `R$ ${formatCurrency(freteAtual)}`;
        document.getElementById('descontoValor').innerText = `R$ ${formatCurrency(desconto)}`;
        document.getElementById('totalCompra').innerText = formatCurrency(total);

        recalcularDivisao(total);
    }

    function obterCuponsSelecionados() {
        return Array.from(cupomCheckboxes)
            .filter((checkbox) => checkbox.checked)
            .map((checkbox) => checkbox.value);
    }

    function exibirMensagemCupom(mensagem, tipo = 'info') {
        if (!cupomMensagem) {
            return;
        }

        if (!mensagem) {
            cupomMensagem.style.display = 'none';
            cupomMensagem.textContent = '';
            cupomMensagem.className = 'checkout-coupon-message';
            return;
        }

        cupomMensagem.style.display = 'block';
        cupomMensagem.textContent = mensagem;
        cupomMensagem.className = `checkout-coupon-message ${tipo}`;
    }

    function recalcularDivisao(total) {
        if (!valor1Input || !valor2Input) {
            return;
        }

        const valor1 = parseDecimal(valor1Input.value);

        if (!segundoPagamentoAtivo || !metodo2Select?.value) {
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

    function obterEstadoSelecionado() {
        const selecionado = document.querySelector("input[name='EnderecoId']:checked");
        if (!selecionado) {
            return '';
        }

        if (selecionado.value === '0') {
            return estadoNovoEnderecoInput?.value?.trim() || '';
        }

        return selecionado.dataset.estado || '';
    }

    async function atualizarFrete() {
        if (!freightUrl) {
            atualizarResumo();
            return;
        }

        const selecionado = document.querySelector("input[name='EnderecoId']:checked");
        const quantidade = getQuantidade();
        const enderecoId = selecionado && selecionado.value !== '0' ? selecionado.value : '';
        const estado = obterEstadoSelecionado();
        const url = `${freightUrl}?enderecoId=${encodeURIComponent(enderecoId)}&estado=${encodeURIComponent(estado)}&quantidade=${encodeURIComponent(quantidade)}`;

        try {
            const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const data = await response.json();

            if (data?.sucesso) {
                freteAtual = parseFloat(data.frete || 0);
            }
        } catch (_) {
        }

        if (cupomAplicadoCodigo || obterCuponsSelecionados().length > 0) {
            await aplicarCupom();
            return;
        }

        atualizarResumo();
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

    function atualizarSegundoPagamentoUI() {
        if (!segundoPagamentoWrapper || !toggleSegundoPagamentoBtn || !metodo2Select) {
            return;
        }

        segundoPagamentoWrapper.style.display = segundoPagamentoAtivo ? 'block' : 'none';
        toggleSegundoPagamentoBtn.textContent = segundoPagamentoAtivo
            ? 'Remover segundo meio de pagamento'
            : 'Adicionar segundo meio de pagamento';
        toggleSegundoPagamentoBtn.setAttribute('aria-expanded', segundoPagamentoAtivo ? 'true' : 'false');

        if (!segundoPagamentoAtivo) {
            metodo2Select.value = '';
            if (valor2Input) {
                valor2Input.value = '';
            }
            togglePagamento(2);
        } else {
            togglePagamento(2);
            recalcularDivisao(obterTotalAtual());
        }
    }

    function toggleEntregaProgramada() {
        if (!tipoEntregaSelect || !dataEntregaProgramadaWrapper) {
            return;
        }

        const entregaProgramadaAtiva = tipoEntregaSelect.value === 'PROGRAMADA';
        dataEntregaProgramadaWrapper.style.display = entregaProgramadaAtiva ? 'grid' : 'none';
        dataEntregaProgramadaWrapper.classList.toggle('is-visible', entregaProgramadaAtiva);

        if (!entregaProgramadaAtiva && dataEntregaPrevistaInput) {
            dataEntregaPrevistaInput.value = '';
        }
    }

    async function aplicarCupom() {
        if (!cupomInput) {
            return;
        }

        const codigo = cupomInput.value.trim();
        const cuponsSelecionados = obterCuponsSelecionados();

        if (!codigo && cuponsSelecionados.length === 0) {
            descontoAplicado = 0;
            cupomAplicadoCodigo = '';
            exibirMensagemCupom('');
            atualizarResumo();
            return;
        }

        const subtotal = calcularSubtotal();
        const params = new URLSearchParams();
        params.set('codigo', codigo);
        params.set('subtotal', subtotal.toFixed(2));
        params.set('frete', freteAtual.toFixed(2));
        cuponsSelecionados.forEach((cupomId) => params.append('cuponsTrocaSelecionados', cupomId));

        try {
            const response = await fetch(`/Pedido/ValidarCupom?${params.toString()}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const data = await response.json();

            if (data?.valido) {
                descontoAplicado = parseFloat(data.desconto || 0);
                cupomAplicadoCodigo = data.codigo || codigo;
                cupomInput.value = cupomAplicadoCodigo;
                exibirMensagemCupom(data?.mensagem || 'Cupom aplicado com sucesso.', 'success');

                if (Array.isArray(data?.cuponsTrocaAplicados)) {
                    const aplicados = data.cuponsTrocaAplicados.map((id) => String(id));
                    cupomCheckboxes.forEach((checkbox) => {
                        checkbox.checked = aplicados.includes(checkbox.value);
                    });
                }
            } else {
                descontoAplicado = 0;
                cupomAplicadoCodigo = '';
                exibirMensagemCupom(data?.mensagem || 'Cupom invalido ou indisponivel.', 'error');
            }
        } catch (_) {
            descontoAplicado = 0;
            cupomAplicadoCodigo = '';
            exibirMensagemCupom('Nao foi possivel validar o cupom agora.', 'error');
        }

        atualizarResumo();
    }

    quantidadeInput?.addEventListener('input', atualizarFrete);
    aplicarCupomBtn?.addEventListener('click', aplicarCupom);
    cupomCheckboxes.forEach((checkbox) => checkbox.addEventListener('change', aplicarCupom));
    valor1Input?.addEventListener('input', () => recalcularDivisao(obterTotalAtual()));
    valor2Input?.addEventListener('input', () => recalcularDivisao(obterTotalAtual()));
    toggleSegundoPagamentoBtn?.addEventListener('click', () => {
        segundoPagamentoAtivo = !segundoPagamentoAtivo;
        atualizarSegundoPagamentoUI();
    });

    enderecoRadios.forEach((radio) => radio.addEventListener('change', async () => {
        toggleNovoEndereco();
        await atualizarFrete();
    }));
    metodoSelects.forEach((select) => select.addEventListener('change', () => togglePagamento(select.dataset.index)));
    cardSelects.forEach((select) => select.addEventListener('change', () => toggleNovoCartao(select.dataset.index)));
    estadoNovoEnderecoInput?.addEventListener('input', atualizarFrete);
    tipoEntregaSelect?.addEventListener('change', toggleEntregaProgramada);

    toggleNovoEndereco();
    togglePagamento(1);
    atualizarSegundoPagamentoUI();
    toggleEntregaProgramada();
    atualizarFrete();
}
