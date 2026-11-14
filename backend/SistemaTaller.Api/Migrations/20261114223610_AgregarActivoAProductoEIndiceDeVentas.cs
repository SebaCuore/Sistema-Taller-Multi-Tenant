using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaTaller.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarActivoAProductoEIndiceDeVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ventas_TallerId",
                table: "Ventas");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Productos",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_TallerId_Fecha",
                table: "Ventas",
                columns: new[] { "TallerId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ventas_TallerId_Fecha",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Productos");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_TallerId",
                table: "Ventas",
                column: "TallerId");
        }
    }
}
