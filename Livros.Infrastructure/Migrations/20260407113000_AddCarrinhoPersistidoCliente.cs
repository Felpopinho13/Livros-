using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Livros.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260407113000_AddCarrinhoPersistidoCliente")]
    public partial class AddCarrinhoPersistidoCliente : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarrinhoPersistidoJson",
                table: "Clientes",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarrinhoPersistidoJson",
                table: "Clientes");
        }
    }
}
