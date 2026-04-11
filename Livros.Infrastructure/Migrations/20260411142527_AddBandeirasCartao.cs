using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Livros.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBandeirasCartao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BandeiraCartaoId",
                table: "Cartoes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "BandeirasCartao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsAtiva = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BandeirasCartao", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BandeirasCartao",
                columns: new[] { "Id", "Codigo", "IsAtiva", "Nome" },
                values: new object[,]
                {
                    { 1, "VISA", true, "Visa" },
                    { 2, "MASTERCARD", true, "Mastercard" },
                    { 3, "ELO", true, "Elo" },
                    { 4, "HIPERCARD", true, "Hipercard" },
                    { 5, "AMEX", true, "American Express" }
                });

            migrationBuilder.Sql("UPDATE Cartoes SET BandeiraCartaoId = 1 WHERE ISNULL(BandeiraCartaoId, 0) = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Cartoes_BandeiraCartaoId",
                table: "Cartoes",
                column: "BandeiraCartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_BandeirasCartao_Codigo",
                table: "BandeirasCartao",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cartoes_BandeirasCartao_BandeiraCartaoId",
                table: "Cartoes",
                column: "BandeiraCartaoId",
                principalTable: "BandeirasCartao",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cartoes_BandeirasCartao_BandeiraCartaoId",
                table: "Cartoes");

            migrationBuilder.DropTable(
                name: "BandeirasCartao");

            migrationBuilder.DropIndex(
                name: "IX_Cartoes_BandeiraCartaoId",
                table: "Cartoes");

            migrationBuilder.DropColumn(
                name: "BandeiraCartaoId",
                table: "Cartoes");
        }
    }
}
