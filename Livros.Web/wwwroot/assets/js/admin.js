// ===============================
// MODAL CLIENTE
// ===============================

function abrirModalCliente() {
    document.getElementById('clienteModal').style.display = 'flex';
}

function fecharModalCliente() {
    document.getElementById('clienteModal').style.display = 'none';
}

// ===============================
// MODAL VER CLIENTE
// ===============================

function abrirModalVer(cliente) {

    document.getElementById("verGenero").value = cliente.genero;
    document.getElementById("verNome").value = cliente.nome;
    document.getElementById("verDataNascimento").value = cliente.dataNascimento;
    document.getElementById("verCpf").value = cliente.cpf;

    document.getElementById("verTelefoneTipo").value = cliente.telefone.tipo;
    document.getElementById("verTelefoneDDD").value = cliente.telefone.ddd;
    document.getElementById("verTelefoneNumero").value = cliente.telefone.numero;

    document.getElementById("verEmail").value = cliente.email;
    document.getElementById("verStatus").value = cliente.status;

    document.getElementById("verCep").value = cliente.endereco.cep;
    document.getElementById("verLogradouro").value = cliente.endereco.logradouro;
    document.getElementById("verNumero").value = cliente.endereco.numero;
    document.getElementById("verBairro").value = cliente.endereco.bairro;
    document.getElementById("verCidade").value = cliente.endereco.cidade;
    document.getElementById("verEstado").value = cliente.endereco.estado;

    document.getElementById("verClienteModal").style.display = "flex";
}

function fecharModalVer() {
    document.getElementById('verClienteModal').style.display = 'none';
}

// ===============================
// MODAL EDITAR CLIENTE
// ===============================

function abrirModalEditar(cliente) {

    document.getElementById("editarClienteId").value = cliente.id;
    document.getElementById("editarGenero").value = cliente.genero;
    document.getElementById("editarNome").value = cliente.nome;
    document.getElementById("editarDataNascimento").value = cliente.dataNascimento;
    document.getElementById("editarCpf").value = cliente.cpf;

    document.getElementById("editarTelefoneTipo").value = cliente.telefone.tipo;
    document.getElementById("editarTelefoneDDD").value = cliente.telefone.ddd;
    document.getElementById("editarTelefoneNumero").value = cliente.telefone.numero;

    document.getElementById("editarEmail").value = cliente.email;
    document.getElementById("editarSenha").value = cliente.senha;

    document.getElementById("editarCep").value = cliente.endereco.cep;
    document.getElementById("editarLogradouro").value = cliente.endereco.logradouro;
    document.getElementById("editarNumero").value = cliente.endereco.numero;
    document.getElementById("editarBairro").value = cliente.endereco.bairro;
    document.getElementById("editarCidade").value = cliente.endereco.cidade;
    document.getElementById("editarEstado").value = cliente.endereco.estado;

    document.getElementById("editarStatus").value = cliente.status;

    document.getElementById("editarClienteModal").style.display = "flex";
}

function fecharModalEditar() {
    document.getElementById('editarClienteModal').style.display = 'none';
}

// ===============================
// MODAL NOVO LIVRO
// ===============================

function abrirModalNovoLivro() {
    document.getElementById('novoLivroModal').style.display = 'flex';
}

function fecharModalNovoLivro() {
    document.getElementById('novoLivroModal').style.display = 'none';
}

// ===============================
// MODAL EDITAR LIVRO
// ===============================

function abrirModalEditarLivro(id, titulo, autor, categoria, isbn, grupo, estoque) {

    document.getElementById("editarLivroId").value = id;
    document.getElementById("editarLivroTitulo").value = titulo;
    document.getElementById("editarLivroAutor").value = autor;
    document.getElementById("editarLivroCategoria").value = categoria;
    document.getElementById("editarLivroISBN").value = isbn;
    document.getElementById("editarLivroGrupo").value = grupo;
    document.getElementById("editarLivroEstoque").value = estoque;

    document.getElementById("editarLivroModal").style.display = "flex";
}

function fecharModalEditarLivro() {
    document.getElementById('editarLivroModal').style.display = 'none';
}

// ===============================
// MODAL ADICIONAR ESTOQUE
// ===============================

function abrirModalAdicionarEstoque(nomeLivro) {
    document.getElementById('livroAdicionarNome').innerText = nomeLivro;
    document.getElementById('quantidadeAdicionar').value = '';
    document.getElementById('adicionarEstoqueModal').style.display = 'flex';
}

function fecharModalAdicionarEstoque() {
    document.getElementById('adicionarEstoqueModal').style.display = 'none';
}

// ===============================
// MODAL AJUSTAR ESTOQUE
// ===============================

function abrirModalAjustarEstoque(nomeLivro, estoqueAtual) {
    document.getElementById('livroAjustarNome').innerText = nomeLivro;
    document.getElementById('novoEstoque').value = estoqueAtual;
    document.getElementById('ajustarEstoqueModal').style.display = 'flex';
}

function fecharModalAjustarEstoque() {
    document.getElementById('ajustarEstoqueModal').style.display = 'none';
}

// ===============================
// MODAL DETALHES PEDIDO
// ===============================

function abrirModalPedido(numero, cliente, data, status, total) {

    document.getElementById('pedidoNumero').innerText = numero;
    document.getElementById('pedidoCliente').innerText = cliente;
    document.getElementById('pedidoData').innerText = data;
    document.getElementById('pedidoStatus').innerText = status;
    document.getElementById('pedidoTotal').innerText = total;

    const tabelaItens = document.getElementById('pedidoItens');
    tabelaItens.innerHTML = `
        <tr>
            <td>Clean Code</td>
            <td>1</td>
            <td>R$ 129,90</td>
            <td>R$ 129,90</td>
        </tr>
        <tr>
            <td>Refactoring</td>
            <td>1</td>
            <td>R$ 60,00</td>
            <td>R$ 60,00</td>
        </tr>
    `;

    document.getElementById('pedidoDetalheModal').style.display = 'flex';
}

function fecharModalPedido() {
    document.getElementById('pedidoDetalheModal').style.display = 'none';
}

// ===============================
// TROCAS
// ===============================

function abrirModalTroca(numero, pedido, cliente, motivo) {
    document.getElementById('trocaNumero').innerText = numero;
    document.getElementById('trocaPedido').innerText = pedido;
    document.getElementById('trocaCliente').innerText = cliente;
    document.getElementById('trocaMotivo').innerText = motivo;

    document.getElementById('analisarTrocaModal').style.display = 'flex';
}

function fecharModalTroca() {
    document.getElementById('analisarTrocaModal').style.display = 'none';
}

function abrirModalVerTroca(numero, pedido, cliente, motivo, status, data) {
    document.getElementById('verTrocaNumero').innerText = numero;
    document.getElementById('verTrocaPedido').innerText = pedido;
    document.getElementById('verTrocaCliente').innerText = cliente;
    document.getElementById('verTrocaMotivo').innerText = motivo;
    document.getElementById('verTrocaStatus').innerText = status;
    document.getElementById('verTrocaData').innerText = data;

    document.getElementById('verTrocaModal').style.display = 'flex';
}

function fecharModalVerTroca() {
    document.getElementById('verTrocaModal').style.display = 'none';
}

const ctx = document.getElementById('salesChart');

if (ctx && typeof Chart !== "undefined") {

    let salesChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: ['2026-01-01', '2026-01-02'],
            datasets: [{
                label: 'Faturamento (R$)',
                data: [15000, 21000],
                borderColor: '#ff9900',
                backgroundColor: 'rgba(255,153,0,0.1)',
                borderWidth: 3,
                tension: 0.3,
                fill: true,
                pointRadius: 5,
                pointBackgroundColor: '#ff9900'
            }]
        },
        options: {
            responsive: true,
            plugins: { legend: { display: false } },
            scales: { y: { beginAtZero: true } }
        }
    });

    const btnFiltrar = document.getElementById("btnFiltrar");

    if (btnFiltrar) {

        btnFiltrar.addEventListener("click", function () {

            const inicio = document.getElementById("dataInicio").value;
            const fim = document.getElementById("dataFim").value;

            if (!inicio || !fim) {
                alert("Selecione as duas datas.");
                return;
            }

            const valor1 = Math.floor(Math.random() * 20000) + 10000;
            const valor2 = Math.floor(Math.random() * 20000) + 10000;

            salesChart.data.labels = [inicio, fim];
            salesChart.data.datasets[0].data = [valor1, valor2];

            salesChart.update();
        });

    }

}

function salvarLivro() {

    const livro = {
        titulo: document.getElementById("livroTitulo").value,
        autor: document.getElementById("livroAutor").value,
        categoria: document.getElementById("livroCategoria").value,
        ano: document.getElementById("livroAno").value,
        editora: document.getElementById("livroEditora").value,
        edicao: document.getElementById("livroEdicao").value,
        isbn: document.getElementById("livroISBN").value,
        codigoBarras: document.getElementById("livroCodigoBarras").value,
        paginas: document.getElementById("livroPaginas").value,
        sinopse: document.getElementById("livroSinopse").value,
        dimensoes: {
            altura: document.getElementById("livroAltura").value,
            largura: document.getElementById("livroLargura").value,
            profundidade: document.getElementById("livroProfundidade").value,
            peso: document.getElementById("livroPeso").value
        },
        grupoPrecificacao: document.getElementById("livroGrupoPrecificacao").value
    };

    console.log(livro);
}

function salvarCliente() {

    const cliente = {
        genero: document.getElementById("clienteGenero").value,
        nome: document.getElementById("clienteNome").value,
        dataNascimento: document.getElementById("clienteDataNascimento").value,
        cpf: document.getElementById("clienteCpf").value,
        telefone: {
            tipo: document.getElementById("clienteTelefoneTipo").value,
            ddd: document.getElementById("clienteTelefoneDDD").value,
            numero: document.getElementById("clienteTelefoneNumero").value
        },
        email: document.getElementById("clienteEmail").value,
        senha: document.getElementById("clienteSenha").value,
        endereco: {
            cep: document.getElementById("clienteCep").value,
            logradouro: document.getElementById("clienteLogradouro").value,
            numero: document.getElementById("clienteNumero").value,
            bairro: document.getElementById("clienteBairro").value,
            cidade: document.getElementById("clienteCidade").value,
            estado: document.getElementById("clienteEstado").value
        }
    };

    console.log("Novo cliente:", cliente);

    fecharModalCliente();
}

function salvarEdicaoCliente() {

    const clienteEditado = {
        id: document.getElementById("editarClienteId").value,
        genero: document.getElementById("editarGenero").value,
        nome: document.getElementById("editarNome").value,
        dataNascimento: document.getElementById("editarDataNascimento").value,
        cpf: document.getElementById("editarCpf").value,
        telefone: {
            tipo: document.getElementById("editarTelefoneTipo").value,
            ddd: document.getElementById("editarTelefoneDDD").value,
            numero: document.getElementById("editarTelefoneNumero").value
        },
        email: document.getElementById("editarEmail").value,
        senha: document.getElementById("editarSenha").value,
        endereco: {
            cep: document.getElementById("editarCep").value,
            logradouro: document.getElementById("editarLogradouro").value,
            numero: document.getElementById("editarNumero").value,
            bairro: document.getElementById("editarBairro").value,
            cidade: document.getElementById("editarCidade").value,
            estado: document.getElementById("editarEstado").value
        },
        status: document.getElementById("editarStatus").value
    };

    console.log("Cliente editado:", clienteEditado);

    fecharModalEditar();
}