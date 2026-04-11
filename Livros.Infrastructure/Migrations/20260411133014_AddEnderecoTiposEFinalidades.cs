using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Livros.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnderecoTiposEFinalidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCobranca",
                table: "Enderecos",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEntrega",
                table: "Enderecos",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Pais",
                table: "Enderecos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Brasil");

            migrationBuilder.AddColumn<string>(
                name: "TipoLogradouro",
                table: "Enderecos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Rua");

            migrationBuilder.AddColumn<string>(
                name: "TipoResidencia",
                table: "Enderecos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Casa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCobranca",
                table: "Enderecos");

            migrationBuilder.DropColumn(
                name: "IsEntrega",
                table: "Enderecos");

            migrationBuilder.DropColumn(
                name: "Pais",
                table: "Enderecos");

            migrationBuilder.DropColumn(
                name: "TipoLogradouro",
                table: "Enderecos");

            migrationBuilder.DropColumn(
                name: "TipoResidencia",
                table: "Enderecos");
        }
    }
}
