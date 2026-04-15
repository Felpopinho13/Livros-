using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Livros.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReservaCarrinho : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservasCarrinho",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LivroId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    SessionKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    ReservadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasCarrinho", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservasCarrinho_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReservasCarrinho_Livros_LivroId",
                        column: x => x.LivroId,
                        principalTable: "Livros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex(
                name: "IX_ReservasCarrinho_ClienteId_SessionKey",
                table: "ReservasCarrinho",
                columns: new[] { "ClienteId", "SessionKey" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservasCarrinho_LivroId_ExpiraEm",
                table: "ReservasCarrinho",
                columns: new[] { "LivroId", "ExpiraEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservasCarrinho");
        }
    }
}


