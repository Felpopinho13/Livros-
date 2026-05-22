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
    const estadoNovoEnderecoInput = document.getElementById('estadoNovoEndereco');
    const tipoEntregaSelect = document.getElementById('tipoEntrega');
    const dataEntregaProgramadaWrapper = document.getElementById('dataEntregaProgramadaWrapper');
    const dataEntregaPrevistaInput = document.getElementById('dataEntregaPrevista');

    const paymentIndexes = [1, 2, 3, 4];
    const optionalPaymentIndexes = [2, 3, 4];
    const valorInputs = Object.fromEntries(paymentIndexes.map((index) => [index, document.querySelector(`[name='Valor${index}']`)]));
    const metodoSelectByIndex = Object.fromEntries(paymentIndexes.map((index) => [index, document.querySelector(`[name='Metodo${index}']`)]));
    const paymentWrappers = {
        2: document.getElementById('segundoPagamentoWrapper'),
        3: document.getElementById('terceiroPagamentoWrapper'),
        4: document.getElementById('quartoPagamentoWrapper')
    };
    const paymentToggleButtons = {
        2: document.getElementById('toggleSegundoPagamento'),
        3: document.getElementById('toggleTerceiroPagamento'),
        4: document.getElementById('toggleQuartoPagamento')
    };
    const paymentLabels = {
        2: { add: 'Adicionar outro pagamento', remove: 'Remover pagamentos adicionais' },
        3: { add: 'Adicionar mais um pagamento', remove: 'Remover este pagamento adicional' },
        4: { add: 'Adicionar mais um pagamento', remove: 'Remover este pagamento adicional' }
    };

    const unitPrice = parseFloat(checkoutLayout.dataset.unitPrice || '0');
    const subtotalBase = parseFloat(checkoutLayout.dataset.subtotalBase || '0');
    const quantidadeBase = parseInt(checkoutLayout.dataset.quantidadeBase || '1', 10);
    const permiteQuantidade = checkoutLayout.dataset.permiteQuantidade === 'true';
    const freightUrl = checkoutLayout.dataset.freteUrl || '';

    let descontoAplicado = parseDecimal(document.getElementById('descontoValor')?.textContent || '0');
    let cupomAplicadoCodigo = cupomInput?.value?.trim() || '';
    let freteAtual = parseFloat(checkoutLayout.dataset.frete || '0');
    let pagamentosAtivos = {
        2: paymentWrappers[2]?.style.display !== 'none',
        3: paymentWrappers[3]?.style.display !== 'none',
        4: paymentWrappers[4]?.style.display !== 'none'
    };

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

    function obterIndicesPagamentosAtivos() {
        const ativos = [1];
        optionalPaymentIndexes.forEach((index) => {
            if (pagamentosAtivos[index] && metodoSelectByIndex[index]?.value) {
                ativos.push(index);
            }
        });

        return ativos;
    }

    function limparPagamentosInativos() {
        paymentIndexes.forEach((index) => {
            const input = valorInputs[index];
            if (!input) {
                return;
            }

            if (index !== 1 && !pagamentosAtivos[index]) {
                input.value = '';
                return;
            }

            if (!metodoSelectByIndex[index]?.value && index !== 1) {
                input.value = '';
            }
        });
    }

    function obterIndiceAutoCalculo(indiceEditado = null) {
        const indicesAtivos = obterIndicesPagamentosAtivos();
        if (indicesAtivos.length <= 1) {
            return null;
        }

        if (indiceEditado === 1) {
            return null;
        }

        return 1;
    }

    function recalcularDivisao(total, indiceEditado = null) {
        limparPagamentosInativos();

        const indicesAtivos = obterIndicesPagamentosAtivos();
        if (indicesAtivos.length <= 1) {
            preencherPagamentoUnico(total);
            return;
        }

        const indiceAutoCalculo = obterIndiceAutoCalculo(indiceEditado);
        if (!indiceAutoCalculo || !valorInputs[indiceAutoCalculo]) {
            return;
        }

        const somaOutrosPagamentos = indicesAtivos
            .filter((index) => index !== indiceAutoCalculo)
            .reduce((acc, index) => acc + parseDecimal(valorInputs[index]?.value), 0);

        const restante = total - somaOutrosPagamentos;
        valorInputs[indiceAutoCalculo].value = formatInputValue(restante > 0 ? restante : 0);
    }

    function preencherPagamentoUnico(total) {
        const indicesAtivos = obterIndicesPagamentosAtivos();
        if (indicesAtivos.length !== 1 || !valorInputs[1]) {
            return;
        }

        valorInputs[1].value = formatInputValue(total);
    }

    function sugerirValorParaPagamento(index) {
        const input = valorInputs[index];
        if (!input || input.value) {
            return;
        }

        recalcularDivisao(obterTotalAtual(), index);
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
        const metodo = metodoSelectByIndex[index]?.value;
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

    function resetarPagamento(index) {
        pagamentosAtivos[index] = false;
        if (metodoSelectByIndex[index]) {
            metodoSelectByIndex[index].value = '';
        }

        if (valorInputs[index]) {
            valorInputs[index].value = '';
        }

        const cardSelect = document.querySelector(`[name='CartaoId${index}']`);
        if (cardSelect) {
            cardSelect.value = '';
        }

        togglePagamento(index);
    }

    function atualizarPagamentoOpcionalUI(index) {
        const wrapper = paymentWrappers[index];
        const button = paymentToggleButtons[index];

        if (!wrapper || !button) {
            return;
        }

        wrapper.style.display = pagamentosAtivos[index] ? 'block' : 'none';
        button.textContent = pagamentosAtivos[index]
            ? paymentLabels[index].remove
            : paymentLabels[index].add;
        button.setAttribute('aria-expanded', pagamentosAtivos[index] ? 'true' : 'false');

        if (!pagamentosAtivos[index]) {
            resetarPagamento(index);

            if (index === 2) {
                pagamentosAtivos[3] = false;
                pagamentosAtivos[4] = false;
                atualizarPagamentoOpcionalUI(3);
                atualizarPagamentoOpcionalUI(4);
            }

            if (index === 3) {
                pagamentosAtivos[4] = false;
                atualizarPagamentoOpcionalUI(4);
            }
        } else {
            togglePagamento(index);
            sugerirValorParaPagamento(index);
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
    valorInputs[1]?.addEventListener('input', () => recalcularDivisao(obterTotalAtual(), 1));
    optionalPaymentIndexes.forEach((index) => {
        valorInputs[index]?.addEventListener('input', () => recalcularDivisao(obterTotalAtual(), index));
        paymentToggleButtons[index]?.addEventListener('click', () => {
            pagamentosAtivos[index] = !pagamentosAtivos[index];
            atualizarPagamentoOpcionalUI(index);
            recalcularDivisao(obterTotalAtual(), index);
        });
    });

    enderecoRadios.forEach((radio) => radio.addEventListener('change', async () => {
        toggleNovoEndereco();
        await atualizarFrete();
    }));
    metodoSelects.forEach((select) => select.addEventListener('change', () => {
        togglePagamento(select.dataset.index);
        const index = Number(select.dataset.index);
        if (!Number.isNaN(index) && index !== 1) {
            sugerirValorParaPagamento(index);
        }
        recalcularDivisao(obterTotalAtual(), Number.isNaN(index) ? null : index);
    }));
    cardSelects.forEach((select) => select.addEventListener('change', () => toggleNovoCartao(select.dataset.index)));
    estadoNovoEnderecoInput?.addEventListener('input', atualizarFrete);
    tipoEntregaSelect?.addEventListener('change', toggleEntregaProgramada);

    toggleNovoEndereco();
    paymentIndexes.forEach((index) => togglePagamento(index));
    optionalPaymentIndexes.forEach((index) => atualizarPagamentoOpcionalUI(index));
    toggleEntregaProgramada();
    atualizarFrete();
}
