using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Livros.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrocasECupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuponsDesconto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAtivo = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataUtilizacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    PedidoId = table.Column<int>(type: "int", nullable: true),
                    TrocaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuponsDesconto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuponsDesconto_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuponsDesconto_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Trocas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    PedidoItemId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObservacaoCliente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObservacaoAdmin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataSolicitacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAnalise = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CupomDescontoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trocas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trocas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trocas_CuponsDesconto_CupomDescontoId",
                        column: x => x.CupomDescontoId,
                        principalTable: "CuponsDesconto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Trocas_PedidoItens_PedidoItemId",
                        column: x => x.PedidoItemId,
                        principalTable: "PedidoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trocas_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuponsDesconto_ClienteId",
                table: "CuponsDesconto",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_CuponsDesconto_PedidoId",
                table: "CuponsDesconto",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Trocas_ClienteId",
                table: "Trocas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Trocas_CupomDescontoId",
                table: "Trocas",
                column: "CupomDescontoId",
                unique: true,
                filter: "[CupomDescontoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Trocas_PedidoId",
                table: "Trocas",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Trocas_PedidoItemId",
                table: "Trocas",
                column: "PedidoItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trocas");

            migrationBuilder.DropTable(
                name: "CuponsDesconto");
        }
    }
}
