using System.Text;
using Livros.Domain;
using Livros.Application.Checkout;
using Livros.Application.CustomerCart;
using Livros.Application.CustomerCheckout;
using Livros.Application.CustomerOrders;
using Livros.Infrastructure.Data;
using Livros.Infrastructure.Services;
using Livros.Web.Controllers;
using Livros.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace Livros.Tests;

public class PedidoControllerTests {
    [Fact]
    public void FinalizarPedido_DeveRegistrarPedidoDeVendaComSucesso_EComEntregaPrevistaProgramada() {
        using var context = CriarContexto();
        var cliente = CriarCliente(context);
        var endereco = CriarEndereco(context, cliente.Id);
        var livro = CriarLivroComEstoque(context);

        var controller = CriarController(context, cliente.Id, "32,00");
        var totalEsperado = 32m; // Livro R$ 20,00 + frete SP R$ 12,00
        var dataEntregaPrevista = DateTime.Today.AddDays(8);

        var form = new CheckoutFormData {
            LivroId = livro.Id,
            Quantidade = 1,
            EnderecoId = endereco.Id,
            TipoEntrega = "PROGRAMADA",
            DataEntregaPrevista = dataEntregaPrevista,
            Metodo1 = "pix",
            Valor1 = totalEsperado
        };

        var resultado = controller.FinalizarPedido(form);

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("PedidoConfirmado", redirect.ActionName);
        Assert.NotNull(redirect.RouteValues);
        Assert.True(redirect.RouteValues!.ContainsKey("id"));

        var pedido = context.Pedidos
            .Include(p => p.Itens)
            .Include(p => p.Pagamentos)
            .Single();

        Assert.Equal(cliente.Id, pedido.ClienteId);
        Assert.Equal(endereco.Id, pedido.EnderecoId);
        Assert.Equal("APROVADA", pedido.Status);
        Assert.Equal("PROGRAMADA", pedido.TipoEntrega);
        Assert.Equal(dataEntregaPrevista.Date, pedido.DataEntregaPrevista?.Date);
        Assert.Equal(totalEsperado, pedido.Total);
        Assert.Single(pedido.Itens);
        Assert.Single(pedido.Pagamentos);

        var item = pedido.Itens.Single();
        Assert.Equal(livro.Id, item.LivroId);
        Assert.Equal(1, item.Quantidade);
        Assert.Equal(livro.Preco, item.PrecoUnitario);

        var pagamento = pedido.Pagamentos.Single();
        Assert.Equal("pix", pagamento.Metodo);
        Assert.Equal(totalEsperado, pagamento.Valor);
        Assert.Equal("Pendente", pagamento.Status);
    }

    [Fact]
    public void FinalizarPedido_NaoDeveRegistrarPedidoQuandoEntregaProgramadaEstiverNoPassado() {
        using var context = CriarContexto();
        var cliente = CriarCliente(context);
        var endereco = CriarEndereco(context, cliente.Id);
        var livro = CriarLivroComEstoque(context);

        var controller = CriarController(context, cliente.Id, "32,00");
        var form = new CheckoutFormData {
            LivroId = livro.Id,
            Quantidade = 1,
            EnderecoId = endereco.Id,
            TipoEntrega = "PROGRAMADA",
            // DataEntregaPrevista = DateTime.Today.AddDays(-1),
            DataEntregaPrevista = new DateTime(2025, 10, 6),
            Metodo1 = "pix",
            Valor1 = 32m
        };

        var resultado = controller.FinalizarPedido(form);

        var viewResult = Assert.IsType<ViewResult>(resultado);
        Assert.Equal("Checkout", viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(CheckoutFormData.DataEntregaPrevista)));
        Assert.Empty(context.Pedidos);
    }

    [Fact]
    public void FinalizarPedido_ComNovoEnderecoSemSalvarNaoDeveExibirEnderecoComoSalvoNoPerfil() {
        using var context = CriarContexto();
        var cliente = CriarCliente(context);
        var enderecoSalvo = CriarEndereco(context, cliente.Id);
        var livro = CriarLivroComEstoque(context);

        var controller = CriarController(context, cliente.Id, "32,00");
        var form = new CheckoutFormData {
            LivroId = livro.Id,
            Quantidade = 1,
            EnderecoId = 0,
            NomeEndereco = "Endereco do pedido",
            CEP = "01001-000",
            TipoLogradouro = "Rua",
            Logradouro = "Rua Nova",
            Numero = "200",
            Complemento = "Casa 2",
            TipoResidencia = "Casa",
            Pais = "Brasil",
            Bairro = "Centro",
            Cidade = "Sao Paulo",
            Estado = "SP",
            SalvarNovoEndereco = false,
            TipoEntrega = "PROGRAMADA",
            DataEntregaPrevista = DateTime.Today.AddDays(8),
            Metodo1 = "pix",
            Valor1 = 32m
        };

        var resultado = controller.FinalizarPedido(form);

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("PedidoConfirmado", redirect.ActionName);

        var pedido = context.Pedidos.Single();
        var enderecosCliente = context.Enderecos
            .Where(e => e.ClienteId == cliente.Id)
            .OrderBy(e => e.Id)
            .ToList();

        Assert.Equal(2, enderecosCliente.Count);

        var enderecoDoPedido = Assert.Single(enderecosCliente, e => e.Id != enderecoSalvo.Id);
        Assert.Equal(enderecoDoPedido.Id, pedido.EnderecoId);
        Assert.False(enderecoDoPedido.IsEntrega);
        Assert.False(enderecoDoPedido.IsCobranca);
        Assert.False(enderecoDoPedido.IsPadrao);

        var enderecosSalvos = new EnderecoService(context).ListarPorCliente(cliente.Id);
        var unicoEnderecoSalvo = Assert.Single(enderecosSalvos);
        Assert.Equal(enderecoSalvo.Id, unicoEnderecoSalvo.Id);
    }

    [Fact]
    public void FinalizarPedido_ComNovoEnderecoESalvarDeveManterEnderecoDisponivelNoPerfil() {
        using var context = CriarContexto();
        var cliente = CriarCliente(context);
        var enderecoSalvo = CriarEndereco(context, cliente.Id);
        var livro = CriarLivroComEstoque(context);

        var controller = CriarController(context, cliente.Id, "32,00");
        var form = new CheckoutFormData {
            LivroId = livro.Id,
            Quantidade = 1,
            EnderecoId = 0,
            NomeEndereco = "Endereco salvo",
            CEP = "01001-000",
            TipoLogradouro = "Rua",
            Logradouro = "Rua Nova",
            Numero = "300",
            Complemento = "Apto 5",
            TipoResidencia = "Apartamento",
            Pais = "Brasil",
            Bairro = "Centro",
            Cidade = "Sao Paulo",
            Estado = "SP",
            SalvarNovoEndereco = true,
            TipoEntrega = "PROGRAMADA",
            DataEntregaPrevista = DateTime.Today.AddDays(8),
            Metodo1 = "pix",
            Valor1 = 32m
        };

        var resultado = controller.FinalizarPedido(form);

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("PedidoConfirmado", redirect.ActionName);

        var enderecosCliente = context.Enderecos
            .Where(e => e.ClienteId == cliente.Id)
            .OrderBy(e => e.Id)
            .ToList();

        Assert.Equal(2, enderecosCliente.Count);

        var novoEnderecoSalvo = Assert.Single(enderecosCliente, e => e.Id != enderecoSalvo.Id);
        Assert.True(novoEnderecoSalvo.IsEntrega);
        Assert.False(novoEnderecoSalvo.IsCobranca);
        Assert.False(novoEnderecoSalvo.IsPadrao);

        var enderecosSalvos = new EnderecoService(context).ListarPorCliente(cliente.Id);
        Assert.Equal(2, enderecosSalvos.Count);
        Assert.Contains(enderecosSalvos, e => e.Id == enderecoSalvo.Id);
        Assert.Contains(enderecosSalvos, e => e.Id == novoEnderecoSalvo.Id);
    }

    private static AppDbContext CriarContexto() {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"livros-finalizar-pedido-{Guid.NewGuid()}")
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static Cliente CriarCliente(AppDbContext context) {
        var cliente = new Cliente {
            Nome = "Cliente Teste",
            Email = "cliente@teste.com",
            Senha = "Senha@123",
            CPF = "12345678901",
            Telefone = "11999999999",
            Genero = "Masculino",
            DataNascimento = new DateTime(2000, 1, 1),
            IsAtivo = true
        };

        context.Clientes.Add(cliente);
        context.SaveChanges();
        return cliente;
    }

    private static Endereco CriarEndereco(AppDbContext context, int clienteId) {
        var estado = new Estado { Nome = "Sao Paulo", Sigla = "SP" };
        context.Estados.Add(estado);
        context.SaveChanges();

        var cidade = new Cidade { Nome = "Sao Paulo", EstadoId = estado.Id };
        context.Cidades.Add(cidade);
        context.SaveChanges();

        var bairro = new Bairro { Nome = "Centro", CidadeId = cidade.Id };
        context.Bairros.Add(bairro);
        context.SaveChanges();

        var endereco = new Endereco {
            NomeEndereco = "Casa",
            CEP = "01001000",
            TipoLogradouro = "Rua",
            Logradouro = "Rua Teste",
            Numero = "100",
            Complemento = "Apto 10",
            TipoResidencia = "Apartamento",
            Pais = "Brasil",
            CidadeId = cidade.Id,
            BairroId = bairro.Id,
            ClienteId = clienteId,
            IsPadrao = true,
            IsEntrega = true,
            IsCobranca = true
        };

        context.Enderecos.Add(endereco);
        context.SaveChanges();
        return endereco;
    }

    private static Livro CriarLivroComEstoque(AppDbContext context) {
        var livro = new Livro {
            Titulo = "Livro de Teste",
            Ano = 2024,
            Autor = "Autor Teste",
            Editora = "Editora Teste",
            Edicao = "1a",
            ISBN = "1234567890123",
            CodigoBarras = "3210987654321",
            NumeroPaginas = 250,
            Sinopse = "Sinopse de teste",
            Altura = 20m,
            Largura = 14m,
            Peso = 0.5m,
            Profundidade = 2m,
            Preco = 20m,
            ImagemUrl = "/img/teste.jpg",
            IsAtivo = true
        };

        context.Livros.Add(livro);
        context.SaveChanges();

        context.Estoques.Add(new Estoque {
            LivroId = livro.Id,
            Quantidade = 10,
            QuantidadeMinima = 1
        });
        context.SaveChanges();
        return livro;
    }

    private static PedidoController CriarController(AppDbContext context, int clienteId, string valor1) {
        var pricingService = new CheckoutPricingService(new CheckoutPricingDataProvider(context));
        var cartService = new CustomerCartService(new CustomerCartDataProvider(context));
        var checkoutService = new CustomerCheckoutService(new CustomerCheckoutDataProvider(context), pricingService);
        var orderPlacementService = new CustomerOrderPlacementService(
            new CustomerOrderPlacementDataProvider(context),
            new CheckoutAddressService(new CheckoutAddressDataProvider(context)),
            pricingService,
            new CheckoutPaymentService(new CheckoutPaymentDataProvider(context)),
            new CheckoutOrderService(),
            checkoutService,
            cartService);
        var controller = new PedidoController(
            pricingService,
            cartService,
            checkoutService,
            orderPlacementService,
            new CustomerOrdersService(new CustomerOrdersDataProvider(context)));

        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        session.SetString("ClienteId", clienteId.ToString());
        httpContext.Features.Set<ISessionFeature>(new TestSessionFeature { Session = session });
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues> {
            ["Valor1"] = valor1
        });

        controller.ControllerContext = new ControllerContext {
            HttpContext = httpContext
        };
        return controller;
    }
}






