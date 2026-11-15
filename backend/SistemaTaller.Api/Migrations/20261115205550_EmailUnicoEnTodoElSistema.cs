using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaTaller.Api.Migrations
{
    /// <inheritdoc />
    public partial class EmailUnicoEnTodoElSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_TallerId_Email",
                table: "Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TallerId",
                table: "Usuarios",
                column: "TallerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_TallerId",
                table: "Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TallerId_Email",
                table: "Usuarios",
                columns: new[] { "TallerId", "Email" },
                unique: true);
        }
    }
}
