using System.Globalization;
using Microsoft.Playwright;

namespace Livros.Tests;

public class VendaPlaywrightTests {
    [Fact]
    public async Task DeveRegistrarPedidoComSucessoNoFluxoVisualDeCompra() {
        await ExecutarFluxoCompraVisualAsync("PROGRAMADA", true);
    }

    [Fact]
    public async Task DeveRegistrarPedidoComSucessoNoFluxoVisualDeCompraComEntregaPadrao() {
        await ExecutarFluxoCompraVisualAsync("PADRAO", false);
    }

    [Fact]
    public async Task DeveRegistrarPedidoComSucessoNoFluxoVisualDeCompraComDoisCartoes() {
        await ExecutarFluxoCompraVisualAsync("PADRAO", false, true);
    }

    private static async Task ExecutarFluxoCompraVisualAsync(string tipoEntrega, bool usarEntregaProgramada, bool usarDoisCartoes = false) {
        var baseUrl = Environment.GetEnvironmentVariable("LIVROS_BASE_URL") ?? "https://localhost:44357";
        var dataEntregaProgramada = DateTime.Today.AddDays(8).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var email = $"playwright.{DateTime.Now:yyyyMMddHHmmss}@teste.com";
        var senha = "Livro@Teste123!";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
            Headless = false,
            SlowMo = 850
        });

        var artifactsDir = Path.Combine(AppContext.BaseDirectory, "playwright-artifacts");
        Directory.CreateDirectory(artifactsDir);

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            RecordVideoDir = artifactsDir,
            RecordVideoSize = new RecordVideoSize { Width = 1440, Height = 900 }
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync($"{baseUrl}/Auth/Cadastro", new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await page.Locator("select[name='genero']").SelectOptionAsync(new[] { "Masculino" });
        await page.Locator("input[name='dataNascimento']").FillAsync("2000-01-01");
        await page.Locator("input[name='nome']").FillAsync("Cliente Playwright");
        await page.Locator("input[name='cpf']").FillAsync("12345678901");
        await page.Locator("input[name='telefone']").FillAsync("11999999999");
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

        await page.Locator("input[name='Email']").FillAsync(email);
        await page.Locator("input[name='Senha']").FillAsync(senha);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

        await page.Locator("#tipoEntrega").SelectOptionAsync(new[] { tipoEntrega });
        if (usarEntregaProgramada) {
            await ExpectAsync(page.Locator("#dataEntregaProgramadaWrapper")).ToBeVisibleAsync();
            await page.Locator("#dataEntregaPrevista").FillAsync(dataEntregaProgramada);
            await page.WaitForTimeoutAsync(1200);
        }
        else {
            await ExpectAsync(page.Locator("#dataEntregaProgramadaWrapper")).ToBeHiddenAsync();
            await page.WaitForTimeoutAsync(900);
        }

        var total = (await page.Locator("#totalCompra").InnerTextAsync()).Trim();
        if (usarDoisCartoes) {
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
        else {
            await page.Locator("select[name='Metodo1']").SelectOptionAsync(new[] { "pix" });
            await page.Locator("input[name='Valor1']").FillAsync(total);
        }
        await page.WaitForTimeoutAsync(1200);

        await page.GetByRole(AriaRole.Button, new() { Name = "Finalizar pagamento" }).ClickAsync();
        await page.WaitForURLAsync("**/Pedido/PedidoConfirmado*");

        await ExpectAsync(page.GetByRole(AriaRole.Heading, new() { Name = "Pedido confirmado!" })).ToBeVisibleAsync();
        if (usarEntregaProgramada) {
            await ExpectAsync(page.Locator(".order-info")).ToContainTextAsync("Entrega programada");
            await ExpectAsync(page.Locator(".order-info")).ToContainTextAsync(DateTime.ParseExact(dataEntregaProgramada, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"));
        }
        else {
            await ExpectAsync(page.Locator(".order-info")).ToContainTextAsync("Entrega padrão");
            await ExpectAsync(page.Locator(".order-info")).Not.ToContainTextAsync("Entrega prevista:");
        }
        await page.WaitForTimeoutAsync(2500);
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

    private static ILocatorAssertions ExpectAsync(ILocator locator) => Assertions.Expect(locator);
}
