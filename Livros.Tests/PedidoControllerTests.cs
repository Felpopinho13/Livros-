using System.Text;
using Livros.Domain;
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
    public void FinalizarPedido_DeveRegistrarPedidoDeVendaComSucesso() {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"livros-finalizar-pedido-{Guid.NewGuid()}")
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

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
            ClienteId = cliente.Id,
            IsPadrao = true,
            IsEntrega = true,
            IsCobranca = true
        };

        context.Enderecos.Add(endereco);
        context.SaveChanges();

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

        var controller = new PedidoController(
            context,
            new LivroService(context),
            new EnderecoService(context));

        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        session.SetString("ClienteId", cliente.Id.ToString());
        httpContext.Features.Set<ISessionFeature>(new TestSessionFeature { Session = session });
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues> {
            ["Valor1"] = "32,00"
        });

        controller.ControllerContext = new ControllerContext {
            HttpContext = httpContext
        };

        var totalEsperado = 32m; // Livro R$ 20,00 + frete SP R$ 12,00

        var form = new CheckoutFormData {
            LivroId = livro.Id,
            Quantidade = 1,
            EnderecoId = endereco.Id,
            TipoEntrega = "PADRAO",
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
        Assert.Equal("PADRAO", pedido.TipoEntrega);
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
}
