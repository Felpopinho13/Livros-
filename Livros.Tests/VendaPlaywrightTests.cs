using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Livros.Tests;

public class VendaPlaywrightTests {
    private const string ClienteExistenteEmail = "voceconhecetudo@gmail.com";
    private const string ClienteExistenteSenha = "Fi2830pi@";
    private const string ClienteChatbotEmail = "pereirafelipi13@gmail.com";
    private const string ClienteChatbotSenha = "Fi2830pi@";
    private const string AdminEmail = "admin@admin.com";
    private const string AdminSenha = "123";

    [Fact]
    public async Task DeveRegistrarPedidoComSucessoNoFluxoVisualDeCompra() {
        await ExecutarFluxoCompraVisualAsync(new CenarioCompraVisual {
            TipoEntrega = "PROGRAMADA",
            UsarEntregaProgramada = true
        });
    }

    [Fact]
    public async Task DeveRegistrarPedidoComSucessoNoFluxoVisualDeCompraComEntregaPadrao() {
        await ExecutarFluxoCompraVisualAsync(new CenarioCompraVisual {
            TipoEntrega = "PADRAO"
        });
    }

    [Fact]
    public async Task DeveRegistrarPedidoComDoisCartoesEValidarFluxoAdministrativo() {
        var baseUrl = Environment.GetEnvironmentVariable("LIVROS_BASE_URL") ?? "https://localhost:44357";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
            Headless = false,
            SlowMo = 850
        });

        var artifactsDir = Path.Combine(AppContext.BaseDirectory, "playwright-artifacts");
        Directory.CreateDirectory(artifactsDir);

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1550, Height = 730 },
            RecordVideoDir = artifactsDir,
            RecordVideoSize = new RecordVideoSize { Width = 1550, Height = 730 }
        });

        var page = await context.NewPageAsync();

        var compra = await ExecutarFluxoCompraVisualNaPaginaAsync(page, baseUrl, new CenarioCompraVisual {
            TipoEntrega = "PADRAO",
            UsarDoisCartoes = true,
            EmailLoginExistente = ClienteExistenteEmail,
            SenhaLoginExistente = ClienteExistenteSenha
        });

        await page.GotoAsync($"{baseUrl}/Auth/Logout", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.WaitForTimeoutAsync(1200);

        await FazerLoginAsync(page, baseUrl, AdminEmail, AdminSenha);
        await ValidarPedidoNoAdminAteEntregueAsync(page, baseUrl, compra.PedidoId);
    }

    [Fact]
    public async Task DeveRegistrarPedidoComSucessoNoFluxoVisualDeCompraComNovoEnderecoENovoCartao() {
        await ExecutarFluxoCompraVisualAsync(new CenarioCompraVisual {
            TipoEntrega = "PADRAO",
            UsarNovoEnderecoNoCheckout = true,
            UsarCartao = true,
            EmailLoginExistente = ClienteExistenteEmail,
            SenhaLoginExistente = ClienteExistenteSenha
        });
    }

    [Fact]
    public async Task DeveRegistrarPedidoComCupomECartaoEPermitirSolicitarTroca() {
        var baseUrl = Environment.GetEnvironmentVariable("LIVROS_BASE_URL") ?? "https://localhost:44357";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
            Headless = false,
            SlowMo = 850
        });

        var artifactsDir = Path.Combine(AppContext.BaseDirectory, "playwright-artifacts");
        Directory.CreateDirectory(artifactsDir);

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1550, Height = 730 },
            RecordVideoDir = artifactsDir,
            RecordVideoSize = new RecordVideoSize { Width = 1550, Height = 730 }
        });

        var page = await context.NewPageAsync();

        var compra = await ExecutarFluxoCompraVisualNaPaginaAsync(page, baseUrl, new CenarioCompraVisual {
            TipoEntrega = "PADRAO",
            UsarCartao = true,
            CupomPromocional = "DESCONTO10"
        });

        await page.GotoAsync($"{baseUrl}/Auth/Logout", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.WaitForTimeoutAsync(1200);

        await page.GotoAsync($"{baseUrl}/Auth/Login", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await page.Locator("input[name='Email']").FillAsync("admin@admin.com");
        await page.Locator("input[name='Senha']").FillAsync("123");
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1500);

        await ValidarPedidoNoAdminAteEntregueAsync(page, baseUrl, compra.PedidoId);

        await page.GotoAsync($"{baseUrl}/Auth/Logout", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.WaitForTimeoutAsync(1200);

        await FazerLoginAsync(page, baseUrl, compra.Email, compra.Senha);

        await page.GotoAsync($"{baseUrl}/Pedido/MeusPedidos", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await ExpectAsync(page.GetByRole(AriaRole.Heading, new() { Name = "Seus pedidos" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Ver detalhes" }).First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await ExpectAsync(page.GetByRole(AriaRole.Heading, new() { Name = "Detalhes do pedido" })).ToBeVisibleAsync();
        await ExpectAsync(page.Locator(".delivery-status")).ToContainTextAsync("ENTREGUE");
        await page.WaitForTimeoutAsync(1200);

        await page.GetByRole(AriaRole.Button, new() { Name = "Solicitar troca" }).First.ClickAsync();
        await Assertions.Expect(page.Locator("#trocaModal")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await page.Locator("#trocaMotivo").SelectOptionAsync(new[] { "Arrependimento" });
        await page.Locator("#trocaObservacao").FillAsync("Solicitacao automatizada de troca apos entrega do pedido.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar solicitação" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await ExpectAsync(page.Locator(".checkout-alert-success")).ToContainTextAsync("enviada com sucesso");
        await ExpectAsync(page.Locator(".exchange-chip")).ToContainTextAsync("EM TROCA");
        await page.WaitForTimeoutAsync(1500);

        await page.GotoAsync($"{baseUrl}/Auth/Logout", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.WaitForTimeoutAsync(1200);

        await FazerLoginAsync(page, baseUrl, "admin@admin.com", "123");

        await page.GotoAsync($"{baseUrl}/Admin/Trocas?busca={compra.PedidoId}", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await ExpectAsync(page.Locator(".admin-page-header h2")).ToContainTextAsync("Trocas");

        var linhaTroca = page.Locator(".exchange-table tbody tr").Filter(new() { HasText = $"#{compra.PedidoId}" }).First;
        await AprovarTrocaNoAdminAsync(page, linhaTroca);
        var cupomTrocaGerado = await ConfirmarRecebimentoTrocaNoAdminAsync(page, linhaTroca);
        
        await page.GotoAsync($"{baseUrl}/Auth/Logout", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.WaitForTimeoutAsync(1200);

        await FazerLoginAsync(page, baseUrl, compra.Email, compra.Senha);
        await RealizarCompraComCupomGeradoAsync(page, baseUrl, cupomTrocaGerado);
        await page.WaitForTimeoutAsync(2000);
    }

    [Fact]
    public async Task DeveTestarChatbotDeRecomendacaoEAnaliseDeVendasNoAdmin() {
        var baseUrl = Environment.GetEnvironmentVariable("LIVROS_BASE_URL") ?? "https://localhost:44357";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
            Headless = false,
            SlowMo = 1200
        });

        var artifactsDir = Path.Combine(AppContext.BaseDirectory, "playwright-artifacts");
        Directory.CreateDirectory(artifactsDir);

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1550, Height = 730 },
            RecordVideoDir = artifactsDir,
            RecordVideoSize = new RecordVideoSize { Width = 1550, Height = 730 }
        });

        var page = await context.NewPageAsync();

        await FazerLoginAsync(page, baseUrl, ClienteChatbotEmail, ClienteChatbotSenha);
        await AbrirChatbotAsync(page);

        var respostaAutor = await EnviarPerguntaChatbotAsync(page, "Me recomenda livros de George Orwell");
        Assert.True(await respostaAutor.Cards.CountAsync() > 0, "A recomendacao por autor nao retornou cards de livros.");
        var autoresRespostaAutor = await respostaAutor.Cards.Locator(".chatbot-book-body span").AllInnerTextsAsync();
        Assert.Contains(autoresRespostaAutor, autor => autor.Contains("George Orwell", StringComparison.OrdinalIgnoreCase));

        var respostaCategoria = await EnviarPerguntaChatbotAsync(page, "Me recomenda livros de Ficcao");
        Assert.True(await respostaCategoria.Cards.CountAsync() > 0, "A recomendacao por categoria nao retornou cards de livros.");
        var categoriasResposta = await respostaCategoria.Cards.Locator(".chatbot-book-categories").AllInnerTextsAsync();
        Assert.Contains(categoriasResposta, categoria => categoria.Contains("Fic", StringComparison.OrdinalIgnoreCase));

        var respostaCombinada = await EnviarPerguntaChatbotAsync(page, "Me recomenda livros de George Orwell na categoria Ficcao");
        Assert.True(await respostaCombinada.Cards.CountAsync() > 0, "A recomendacao combinando autor e categoria nao retornou cards de livros.");
        var autoresRespostaCombinada = await respostaCombinada.Cards.Locator(".chatbot-book-body span").AllInnerTextsAsync();
        var categoriasRespostaCombinada = await respostaCombinada.Cards.Locator(".chatbot-book-categories").AllInnerTextsAsync();
        Assert.Contains(autoresRespostaCombinada, autor => autor.Contains("George Orwell", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(categoriasRespostaCombinada, categoria => categoria.Contains("Fic", StringComparison.OrdinalIgnoreCase));

        var respostaInusitada = await EnviarPerguntaChatbotAsync(page, "Me recomenda um livro pra ler nadando");
        var textoInusitado = await respostaInusitada.Bubble.InnerTextAsync();
        Assert.True(
            textoInusitado.Contains("nao parece viavel", StringComparison.OrdinalIgnoreCase)
            || textoInusitado.Contains("momento melhor", StringComparison.OrdinalIgnoreCase)
            || textoInusitado.Contains("momento mais adequado", StringComparison.OrdinalIgnoreCase),
            $"A resposta para o contexto inusitado nao trouxe o desvio esperado. Resposta: {textoInusitado}");

        await page.GotoAsync($"{baseUrl}/Auth/Logout", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.WaitForTimeoutAsync(1000);

        await FazerLoginAsync(page, baseUrl, AdminEmail, AdminSenha);
        await page.GotoAsync($"{baseUrl}/Admin/AnaliseVendas", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await ExpectAsync(page.Locator(".admin-page-header h2")).ToContainTextAsync("Analise de Vendas");

        var dataInicio = DateTime.Today.AddMonths(-3).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var dataFim = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await page.Locator("input[name='dataInicio']").FillAsync(dataInicio);
        await page.Locator("input[name='dataFim']").FillAsync(dataFim);
        await page.Locator("select[name='agrupamento']").SelectOptionAsync(new[] { "mensal" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Atualizar analise" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await ExpectAsync(page.Locator("#salesChart")).ToBeVisibleAsync();
        await ExpectAsync(page.Locator("#categorySalesChart")).ToBeVisibleAsync();
        await ExpectAsync(page.Locator("input[name='dataInicio']")).ToHaveValueAsync(dataInicio);
        await ExpectAsync(page.Locator("input[name='dataFim']")).ToHaveValueAsync(dataFim);

        var seletorCategorias = page.Locator("select[name='categoriasIds']");
        var valorFiccao = await ObterValorOpcaoPorTextoAsync(seletorCategorias, "Ficcao");
        Assert.False(string.IsNullOrWhiteSpace(valorFiccao));

        var valorClassicos = await ObterValorOpcaoPorTextoAsync(seletorCategorias, "Classicos")
            ?? await ObterPrimeiroValorOpcaoDiferenteAsync(seletorCategorias, valorFiccao!);
        Assert.False(string.IsNullOrWhiteSpace(valorClassicos));

        await seletorCategorias.SelectOptionAsync(new[] {
            valorFiccao!,
            valorClassicos!
        });
        await page.GetByRole(AriaRole.Button, new() { Name = "Atualizar analise" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await ExpectAsync(page.Locator("select[name='categoriasIds'] option:checked")).ToHaveCountAsync(2);
        await ExpectAsync(page.Locator(".admin-sales-chart-card").Nth(1)).ToContainTextAsync("Fic");
    }

    private static async Task<ResultadoCompraVisual> ExecutarFluxoCompraVisualAsync(CenarioCompraVisual cenario) {
        var baseUrl = Environment.GetEnvironmentVariable("LIVROS_BASE_URL") ?? "https://localhost:44357";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
            Headless = false,
            SlowMo = 850
        });

        var artifactsDir = Path.Combine(AppContext.BaseDirectory, "playwright-artifacts");
        Directory.CreateDirectory(artifactsDir);

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1550, Height = 730 },
            RecordVideoDir = artifactsDir,
            RecordVideoSize = new RecordVideoSize { Width = 1550, Height = 730 }
        });

        var page = await context.NewPageAsync();

        return await ExecutarFluxoCompraVisualNaPaginaAsync(page, baseUrl, cenario);
    }

    private static async Task<ResultadoCompraVisual> ExecutarFluxoCompraVisualNaPaginaAsync(IPage page, string baseUrl, CenarioCompraVisual cenario) {
        var dataEntregaProgramada = DateTime.Today.AddDays(8).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var identificador = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        var email = !string.IsNullOrWhiteSpace(cenario.EmailLoginExistente)
            ? cenario.EmailLoginExistente
            : $"playwright.{identificador}@teste.com";
        var senha = !string.IsNullOrWhiteSpace(cenario.SenhaLoginExistente)
            ? cenario.SenhaLoginExistente
            : "Livro@Teste123!";
        var cpf = GerarCpfTeste(identificador);
        var telefone = "119" + identificador[^8..];

        if (!string.IsNullOrWhiteSpace(cenario.EmailLoginExistente) && !string.IsNullOrWhiteSpace(cenario.SenhaLoginExistente)) {
            await FazerLoginAsync(page, baseUrl, email, senha);
        }
        else {
            await page.GotoAsync($"{baseUrl}/Auth/Cadastro", new PageGotoOptions {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await page.Locator("select[name='genero']").SelectOptionAsync(new[] { "Masculino" });
            await page.Locator("input[name='dataNascimento']").FillAsync("2000-01-01");
            await page.Locator("input[name='nome']").FillAsync("Cliente Playwright");
            await page.Locator("input[name='cpf']").FillAsync(cpf);
            await page.Locator("input[name='telefone']").FillAsync(telefone);
            await page.Locator("input[name='email']").FillAsync(email);
            await page.Locator("input[name='senha']").FillAsync(senha);
            await page.Locator("input[name='nomeEndereco']").FillAsync("Casa Playwright");
            await page.Locator("input[name='cep']").FillAsync("01001-000");
            await page.Locator("input[name='logradouro']").FillAsync("Rua Teste");
            await page.Locator("select[name='tipoLogradouro']").SelectOptionAsync(new[] { "Rua" });
            await page.Locator("select[name='tipoResidencia']").SelectOptionAsync(new[] { "Casa" });
            await page.Locator("input[name='numero']").FillAsync("100");
            await page.Locator("input[name='complemento']").FillAsync("Casa 1");
            await page.Locator("input[name='bairro']").FillAsync("Centro");
            await page.Locator("input[name='pais']").FillAsync("Brasil");
            await page.Locator("input[name='cidade']").FillAsync("Sao Paulo");
            await page.Locator("select[name='estado']").SelectOptionAsync(new[] { "SP" });

            await page.GetByRole(AriaRole.Button, new() { Name = "Criar Conta" }).ClickAsync();
            await page.WaitForURLAsync("**/Auth/Login");
            await page.WaitForTimeoutAsync(1200);

            await FazerLoginAsync(page, baseUrl, email, senha);
        }

        await ExpectAsync(page.Locator(".product-card").First).ToBeVisibleAsync();
        await page.WaitForTimeoutAsync(1500);

        var adicionarCarrinhoBotao = page.GetByRole(AriaRole.Button, new() { Name = "Adicionar ao carrinho" }).First;
        await adicionarCarrinhoBotao.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1200);

        await page.GotoAsync($"{baseUrl}/Pedido/Carrinho", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.WaitForTimeoutAsync(1500);

        await page.GetByRole(AriaRole.Link, new() { Name = "Fechar pedido" }).ClickAsync();
        await page.WaitForURLAsync("**/Pedido/CheckoutCarrinho");
        await page.WaitForTimeoutAsync(1800);

        if (cenario.UsarNovoEnderecoNoCheckout) {
            await page.Locator(".address-card.novo-endereco").ClickAsync();
            await page.Locator("input[name='EnderecoId'][value='0']").EvaluateAsync(
                "radio => { radio.checked = true; radio.dispatchEvent(new Event('change', { bubbles: true })); }");
            await ExpectAsync(page.Locator("#novoEnderecoForm")).ToBeVisibleAsync();
            var novoEnderecoForm = page.Locator("#novoEnderecoForm");

            await novoEnderecoForm.Locator("input[name='NomeEndereco']").FillAsync("Entrega Alternativa");
            await novoEnderecoForm.Locator("input[name='CEP']").FillAsync("20040-020");
            await novoEnderecoForm.Locator("input[name='Logradouro']").FillAsync("Rua da Quitanda");
            await novoEnderecoForm.Locator("input[name='Numero']").FillAsync("200");
            await novoEnderecoForm.Locator("input[name='Complemento']").FillAsync("Sala 5");
            await novoEnderecoForm.Locator("input[name='Bairro']").FillAsync("Centro");
            await novoEnderecoForm.Locator("input[name='Cidade']").FillAsync("Rio de Janeiro");
            await novoEnderecoForm.Locator("#estadoNovoEndereco").FillAsync("RJ");
            await page.WaitForTimeoutAsync(1200);
        }

        await page.Locator("#tipoEntrega").SelectOptionAsync(new[] { cenario.TipoEntrega });
        if (cenario.UsarEntregaProgramada) {
            await ExpectAsync(page.Locator("#dataEntregaProgramadaWrapper")).ToBeVisibleAsync();
            await page.Locator("#dataEntregaPrevista").FillAsync(dataEntregaProgramada);
            await page.WaitForTimeoutAsync(1200);
        }
        else {
            await ExpectAsync(page.Locator("#dataEntregaProgramadaWrapper")).ToBeHiddenAsync();
            await page.WaitForTimeoutAsync(900);
        }

        if (!string.IsNullOrWhiteSpace(cenario.CupomPromocional)) {
            await page.Locator("#cupom").FillAsync(cenario.CupomPromocional);
            await page.Locator("#aplicarCupomBtn").ClickAsync();
            await ExpectAsync(page.Locator("#cupomMensagem")).ToContainTextAsync("Cupom aplicado com sucesso.");
            await page.WaitForTimeoutAsync(1500);
        }

        var total = (await page.Locator("#totalCompra").InnerTextAsync()).Trim();
        if (cenario.UsarDoisCartoes) {
            await page.Locator("select[name='Metodo1']").SelectOptionAsync(new[] { "cartao" });
            await ExpectAsync(page.Locator("#cartaoForm1")).ToBeVisibleAsync();
            await page.Locator("select[name='BandeiraCartaoId1']").SelectOptionAsync(new[] { "1" });
            await page.Locator("input[name='NomeCartao1']").FillAsync("Cliente Playwright");
            await page.Locator("input[name='NumeroCartao1']").FillAsync("4111111111111111");
            await page.Locator("input[name='CVV1']").FillAsync("123");
            await page.Locator("input[name='Validade1']").FillAsync("12/30");

            await page.GetByRole(AriaRole.Button, new() { Name = "Adicionar segundo meio de pagamento" }).ClickAsync();
            await ExpectAsync(page.Locator("#segundoPagamentoWrapper")).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(1200);

            await page.Locator("select[name='Metodo2']").SelectOptionAsync(new[] { "cartao" });
            await ExpectAsync(page.Locator("#cartaoForm2")).ToBeVisibleAsync();
            await page.Locator("select[name='BandeiraCartaoId2']").SelectOptionAsync(new[] { "2" });
            await page.Locator("input[name='NomeCartao2']").FillAsync("Cliente Playwright 2");
            await page.Locator("input[name='NumeroCartao2']").FillAsync("5555555555554444");
            await page.Locator("input[name='CVV2']").FillAsync("456");
            await page.Locator("input[name='Validade2']").FillAsync("11/31");

            var (valor1, valor2) = DividirTotalParaDoisCartoes(total);
            await page.Locator("input[name='Valor1']").FillAsync(valor1);
            await page.Locator("input[name='Valor2']").FillAsync(valor2);
        }
        else if (cenario.UsarCartao) {
            await page.Locator("select[name='Metodo1']").SelectOptionAsync(new[] { "cartao" });
            await ExpectAsync(page.Locator("#cartaoForm1")).ToBeVisibleAsync();
            await page.Locator("select[name='BandeiraCartaoId1']").SelectOptionAsync(new[] { "1" });
            await page.Locator("input[name='NomeCartao1']").FillAsync("Cliente Playwright");
            await page.Locator("input[name='NumeroCartao1']").FillAsync("4111111111111111");
            await page.Locator("input[name='CVV1']").FillAsync("123");
            await page.Locator("input[name='Validade1']").FillAsync("12/30");
            await page.Locator("input[name='Valor1']").FillAsync(total);
        }
        else {
            await page.Locator("select[name='Metodo1']").SelectOptionAsync(new[] { "pix" });
            await page.Locator("input[name='Valor1']").FillAsync(total);
        }
        await page.WaitForTimeoutAsync(1200);

        await page.GetByRole(AriaRole.Button, new() { Name = "Finalizar pagamento" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await ExpectAsync(page.GetByRole(AriaRole.Heading, new() { Name = "Pedido confirmado!" })).ToBeVisibleAsync();
        if (cenario.UsarEntregaProgramada) {
            await ExpectAsync(page.Locator(".order-info")).ToContainTextAsync("Entrega programada");
            await ExpectAsync(page.Locator(".order-info")).ToContainTextAsync(DateTime.ParseExact(dataEntregaProgramada, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"));
        }
        else {
            await ExpectAsync(page.Locator(".order-info")).ToContainTextAsync("Entrega padrão");
            await ExpectAsync(page.Locator(".order-info")).Not.ToContainTextAsync("Entrega prevista:");
        }
        var pedidoInfo = (await page.Locator(".order-info").InnerTextAsync()).Trim();
        var pedidoId = ExtrairPedidoId(pedidoInfo);
        await page.WaitForTimeoutAsync(2500);

        return new ResultadoCompraVisual {
            PedidoId = pedidoId,
            Email = email,
            Senha = senha
        };
    }

    private static async Task FazerLoginAsync(IPage page, string baseUrl, string email, string senha) {
        await page.GotoAsync($"{baseUrl}/Auth/Login", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await page.Locator("input[name='Email']").FillAsync(email);
        await page.Locator("input[name='Senha']").FillAsync(senha);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1200);
    }

    private static async Task AbrirChatbotAsync(IPage page) {
        var toggle = page.Locator("[data-chatbot-toggle]");
        await ExpectAsync(toggle).ToBeVisibleAsync();
        await toggle.ClickAsync();
        await ExpectAsync(page.Locator("[data-chatbot-panel]")).ToHaveAttributeAsync("aria-hidden", "false");
        await ExpectAsync(page.Locator("[data-chatbot-input]")).ToBeVisibleAsync();
    }

    private static async Task<ChatbotInteractionResult> EnviarPerguntaChatbotAsync(IPage page, string pergunta) {
        var messages = page.Locator("[data-chatbot-messages]");
        var userCountAntes = await page.Locator(".chatbot-message.user").CountAsync();
        var assistantCountAntes = await page.Locator(".chatbot-message.assistant").CountAsync();
        var input = page.Locator("[data-chatbot-input]");

        await input.FillAsync(pergunta);
        var response = await page.RunAndWaitForResponseAsync(
            async () => {
                await input.PressAsync("Enter");
            },
            response => response.Url.Contains("/Chatbot/Recommend", StringComparison.OrdinalIgnoreCase)
                && string.Equals(response.Request.Method, "POST", StringComparison.OrdinalIgnoreCase),
            new() {
                Timeout = 30000
            });

        Assert.True(response.Ok, $"A chamada do chatbot falhou com status HTTP {(int)response.Status}.");

        await Assertions.Expect(page.Locator(".chatbot-message.user")).ToHaveCountAsync(userCountAntes + 1, new() {
            Timeout = 10000
        });
        await Assertions.Expect(page.Locator(".chatbot-message.assistant")).ToHaveCountAsync(assistantCountAntes + 1, new() {
            Timeout = 10000
        });

        var userMessage = page.Locator(".chatbot-message.user").Nth(userCountAntes);
        var assistantMessage = page.Locator(".chatbot-message.assistant").Nth(assistantCountAntes);
        await ExpectAsync(userMessage).ToContainTextAsync(pergunta);
        await ExpectAsync(assistantMessage).ToBeVisibleAsync();
        await ExpectAsync(assistantMessage.Locator(".chatbot-message-bubble")).Not.ToContainTextAsync("Estou analisando o catalogo", new() {
            Timeout = 10000
        });

        var recommendationCards = assistantMessage.Locator(".chatbot-book-card");
        await messages.EvaluateAsync("element => { element.scrollTop = element.scrollHeight; }");
        await page.WaitForTimeoutAsync(800);

        return new ChatbotInteractionResult {
            Bubble = assistantMessage.Locator(".chatbot-message-bubble"),
            Cards = recommendationCards
        };
    }

    private static async Task AtualizarStatusPedidoNoAdminAsync(IPage page, ILocator linhaPedido, string statusAtualEsperado, string novoStatus, string statusPagamentoEsperado) {
        await ExpectAsync(linhaPedido).ToBeVisibleAsync();
        await linhaPedido.ScrollIntoViewIfNeededAsync();
        await ExpectAsync(linhaPedido.Locator(".admin-order-status")).ToContainTextAsync(statusAtualEsperado);

        var actionButton = linhaPedido.Locator(".admin-order-action-btn");
        var abrirModalScript = await actionButton.GetAttributeAsync("onclick");
        if (string.IsNullOrWhiteSpace(abrirModalScript)) {
            throw new InvalidOperationException("Nao foi possivel obter o comando de abertura do modal do pedido.");
        }

        await page.EvaluateAsync("script => { eval(script); }", abrirModalScript);
        await Assertions.Expect(page.Locator("#pedidoModal")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await ExpectAsync(page.Locator("#pedidoStatusAtual")).ToContainTextAsync(statusAtualEsperado);

        await page.Locator("#novoStatusPedido").SelectOptionAsync(new[] { novoStatus });
        await page.WaitForTimeoutAsync(900);
        await page.Locator("#pedidoModalSubmit").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await ExpectAsync(linhaPedido).ToBeVisibleAsync();
        await ExpectAsync(linhaPedido.Locator(".admin-order-payment")).ToContainTextAsync(statusPagamentoEsperado);
        await ExpectAsync(linhaPedido.Locator(".admin-order-status")).ToContainTextAsync(novoStatus);
        await page.WaitForTimeoutAsync(1500);
    }

    private static async Task ValidarPedidoNoAdminAteEntregueAsync(IPage page, string baseUrl, string pedidoId) {
        await page.GotoAsync($"{baseUrl}/Admin/Pedidos?busca={pedidoId}", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await ExpectAsync(page.Locator(".admin-page-header h2")).ToContainTextAsync("Pedidos");

        var linhaPedido = page.Locator("tbody tr").Filter(new() { HasText = $"#{pedidoId}" }).First;
        await ExpectAsync(linhaPedido).ToBeVisibleAsync();
        await linhaPedido.ScrollIntoViewIfNeededAsync();
        await ExpectAsync(linhaPedido.Locator(".admin-order-status")).ToContainTextAsync("APROVADA");
        await page.WaitForTimeoutAsync(1500);

        await AtualizarStatusPedidoNoAdminAsync(page, linhaPedido, "APROVADA", "EM SEPARACAO", "Aprovado");
        await AtualizarStatusPedidoNoAdminAsync(page, linhaPedido, "EM SEPARACAO", "EM TRANSPORTE", "Aprovado");
        await AtualizarStatusPedidoNoAdminAsync(page, linhaPedido, "EM TRANSPORTE", "ENTREGUE", "Aprovado");
    }

    private static async Task AprovarTrocaNoAdminAsync(IPage page, ILocator linhaTroca) {
        await ExpectAsync(linhaTroca).ToBeVisibleAsync();
        await linhaTroca.ScrollIntoViewIfNeededAsync();
        await ExpectAsync(linhaTroca.Locator(".exchange-status")).ToContainTextAsync("EM TROCA");

        var actionButton = linhaTroca.Locator(".exchange-action-btn");
        var abrirModalScript = await actionButton.GetAttributeAsync("onclick");
        if (string.IsNullOrWhiteSpace(abrirModalScript)) {
            throw new InvalidOperationException("Nao foi possivel obter o comando de abertura do modal da troca.");
        }

        await page.EvaluateAsync("script => { eval(script); }", abrirModalScript);
        await Assertions.Expect(page.Locator("#trocaModal")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await ExpectAsync(page.Locator("#trocaStatusAtual")).ToContainTextAsync("EM TROCA");

        await page.Locator("#decisaoTroca").SelectOptionAsync(new[] { "aprovar" });
        await page.Locator("#observacaoAdminTroca").FillAsync("Troca aprovada automaticamente pelo teste Playwright.");
        await page.WaitForTimeoutAsync(900);
        await page.Locator(".exchange-analysis-submit").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await ExpectAsync(linhaTroca).ToBeVisibleAsync();
        await ExpectAsync(page.Locator(".checkout-alert-success")).ToContainTextAsync("autorizada");
        await ExpectAsync(linhaTroca.Locator(".exchange-status")).ToContainTextAsync("TROCA AUTORIZADA");
        await page.WaitForTimeoutAsync(1500);
    }

    private static async Task<string> ConfirmarRecebimentoTrocaNoAdminAsync(IPage page, ILocator linhaTroca) {
        await ExpectAsync(linhaTroca).ToBeVisibleAsync();
        await linhaTroca.ScrollIntoViewIfNeededAsync();
        await ExpectAsync(linhaTroca.Locator(".exchange-status")).ToContainTextAsync("TROCA AUTORIZADA");

        var actionButton = linhaTroca.Locator(".exchange-action-btn");
        var abrirModalScript = await actionButton.GetAttributeAsync("onclick");
        if (string.IsNullOrWhiteSpace(abrirModalScript)) {
            throw new InvalidOperationException("Nao foi possivel obter o comando de abertura do modal de recebimento da troca.");
        }

        await page.EvaluateAsync("script => { eval(script); }", abrirModalScript);
        await Assertions.Expect(page.Locator("#trocaModal")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await ExpectAsync(page.Locator("#trocaStatusAtual")).ToContainTextAsync("TROCA AUTORIZADA");

        await page.Locator("#retornarAoEstoqueTroca").SelectOptionAsync(new[] { "true" });
        await page.Locator("#valorCupomTroca").FillAsync("29,00");
        await page.Locator("#observacaoAdminTroca").FillAsync("Recebimento confirmado automaticamente pelo teste Playwright.");
        await page.WaitForTimeoutAsync(900);
        await page.Locator(".exchange-receipt-submit").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var mensagemSucesso = (await page.Locator(".checkout-alert-success").InnerTextAsync()).Trim();
        await ExpectAsync(linhaTroca).ToBeVisibleAsync();
        await ExpectAsync(page.Locator(".checkout-alert-success")).ToContainTextAsync("cupom");
        await ExpectAsync(linhaTroca.Locator(".exchange-status")).ToContainTextAsync("TROCADO");
        await page.WaitForTimeoutAsync(1500);

        var codigoCupom = ExtrairCodigoCupom(mensagemSucesso);
        return codigoCupom;
    }

    private static async Task RealizarCompraComCupomGeradoAsync(IPage page, string baseUrl, string codigoCupomTroca) {
        await page.GotoAsync(baseUrl, new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await ExpectAsync(page.Locator(".product-card").First).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Adicionar ao carrinho" }).First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1200);

        await page.GotoAsync($"{baseUrl}/Pedido/Carrinho", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.WaitForTimeoutAsync(1200);

        await page.GetByRole(AriaRole.Link, new() { Name = "Fechar pedido" }).ClickAsync();
        await page.WaitForURLAsync("**/Pedido/CheckoutCarrinho");
        await page.WaitForTimeoutAsync(1500);

        await page.Locator("#cupom").FillAsync(codigoCupomTroca);
        await page.Locator("#aplicarCupomBtn").ClickAsync();
        await ExpectAsync(page.Locator("#cupomMensagem")).ToContainTextAsync("Cupom aplicado com sucesso.");
        await page.WaitForTimeoutAsync(1200);

        var totalTexto = (await page.Locator("#totalCompra").InnerTextAsync()).Trim();
        var totalRestante = decimal.Parse(totalTexto, new CultureInfo("pt-BR"));

        if (totalRestante > 0) {
            await page.Locator("select[name='Metodo1']").SelectOptionAsync(new[] { "pix" });
            await page.Locator("input[name='Valor1']").FillAsync(totalRestante.ToString("N2", new CultureInfo("pt-BR")));
            await page.WaitForTimeoutAsync(900);
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Finalizar pagamento" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await ExpectAsync(page.GetByRole(AriaRole.Heading, new() { Name = "Pedido confirmado!" })).ToBeVisibleAsync();
        await ExpectAsync(page.Locator(".order-info")).ToContainTextAsync("Entrega padrÃ£o");
        await page.WaitForTimeoutAsync(1800);
    }

    private sealed class CenarioCompraVisual {
        public string TipoEntrega { get; set; } = "PADRAO";
        public bool UsarEntregaProgramada { get; set; }
        public bool UsarDoisCartoes { get; set; }
        public bool UsarCartao { get; set; }
        public bool UsarNovoEnderecoNoCheckout { get; set; }
        public string? CupomPromocional { get; set; }
        public string? EmailLoginExistente { get; set; }
        public string? SenhaLoginExistente { get; set; }
    }

    private sealed class ResultadoCompraVisual {
        public string PedidoId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    private static string GerarCpfTeste(string identificador) {
        var numeros = new string(identificador.Where(char.IsDigit).ToArray());
        return numeros.Length >= 11 ? numeros[^11..] : numeros.PadLeft(11, '0');
    }

    private static (string valor1, string valor2) DividirTotalParaDoisCartoes(string totalFormatado) {
        var total = decimal.Parse(totalFormatado, new CultureInfo("pt-BR"));
        var valor1 = decimal.Round(total / 2m, 2, MidpointRounding.AwayFromZero);
        if (valor1 < 10m) {
            valor1 = 10m;
        }

        var valor2 = decimal.Round(total - valor1, 2, MidpointRounding.AwayFromZero);
        if (valor2 < 10m) {
            valor2 = 10m;
            valor1 = decimal.Round(total - valor2, 2, MidpointRounding.AwayFromZero);
        }

        return (
            valor1.ToString("N2", new CultureInfo("pt-BR")),
            valor2.ToString("N2", new CultureInfo("pt-BR"))
        );
    }

    private static string ExtrairPedidoId(string pedidoInfo) {
        var match = Regex.Match(pedidoInfo, @"#(?<id>\d+)");
        if (!match.Success) {
            throw new InvalidOperationException("Nao foi possivel localizar o numero do pedido na tela de confirmacao.");
        }

        return match.Groups["id"].Value;
    }

    private static string ExtrairCodigoCupom(string mensagemSucesso) {
        var match = Regex.Match(mensagemSucesso, @"cupom\s+(?<codigo>[A-Z0-9\-]+)", RegexOptions.IgnoreCase);
        if (!match.Success) {
            throw new InvalidOperationException("Nao foi possivel identificar o codigo do cupom gerado na mensagem de sucesso.");
        }

        return match.Groups["codigo"].Value.Trim().TrimEnd('.', ',');
    }

    private static async Task<string?> ObterValorOpcaoPorTextoAsync(ILocator selectLocator, string textoOpcao) {
        return await selectLocator.EvaluateAsync<string?>(
            @"(select, texto) => {
                const normalize = (value) => (value || '')
                    .normalize('NFD')
                    .replace(/[\u0300-\u036f]/g, '')
                    .trim()
                    .toLowerCase();
                const option = Array.from(select.options)
                    .find(item => item.textContent && normalize(item.textContent) === normalize(texto));
                return option ? option.value : null;
            }",
            textoOpcao);
    }

    private static async Task<string?> ObterPrimeiroValorOpcaoDiferenteAsync(ILocator selectLocator, string valorIgnorado) {
        return await selectLocator.EvaluateAsync<string?>(
            @"(select, valor) => {
                const option = Array.from(select.options)
                    .find(item => item.value !== valor);
                return option ? option.value : null;
            }",
            valorIgnorado);
    }

    private sealed class ChatbotInteractionResult {
        public required ILocator Bubble { get; init; }
        public required ILocator Cards { get; init; }
    }

    private static ILocatorAssertions ExpectAsync(ILocator locator) => Assertions.Expect(locator);
}
