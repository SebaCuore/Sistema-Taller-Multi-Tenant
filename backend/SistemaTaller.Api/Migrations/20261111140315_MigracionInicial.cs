using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SistemaTaller.Api.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Talleres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Talleres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposVehiculo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposVehiculo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TallerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    StockActual = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Productos_Talleres_TallerId",
                        column: x => x.TallerId,
                        principalTable: "Talleres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Servicios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TallerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CategoriaVehiculo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AplicaPosicion = table.Column<bool>(type: "boolean", nullable: false),
                    AplicaLado = table.Column<bool>(type: "boolean", nullable: false),
                    PrecioReferencia = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servicios_Talleres_TallerId",
                        column: x => x.TallerId,
                        principalTable: "Talleres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TallerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Talleres_TallerId",
                        column: x => x.TallerId,
                        principalTable: "Talleres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehiculos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TallerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Patente = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TipoVehiculoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Marca = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Modelo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Anio = table.Column<int>(type: "integer", nullable: true),
                    ContactoNombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ContactoTelefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehiculos_Talleres_TallerId",
                        column: x => x.TallerId,
                        principalTable: "Talleres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehiculos_TiposVehiculo_TipoVehiculoId",
                        column: x => x.TipoVehiculoId,
                        principalTable: "TiposVehiculo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TallerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ventas_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ventas_Talleres_TallerId",
                        column: x => x.TallerId,
                        principalTable: "Talleres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesTrabajo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TallerId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehiculoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notas = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesTrabajo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajo_Talleres_TallerId",
                        column: x => x.TallerId,
                        principalTable: "Talleres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajo_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajo_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesTrabajoServicio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TallerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdenTrabajoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Posicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Lado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PrecioFinal = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Notas = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesTrabajoServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajoServicio_OrdenesTrabajo_OrdenTrabajoId",
                        column: x => x.OrdenTrabajoId,
                        principalTable: "OrdenesTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajoServicio_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajoServicio_Talleres_TallerId",
                        column: x => x.TallerId,
                        principalTable: "Talleres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Talleres",
                columns: new[] { "Id", "Activo", "FechaAlta", "Nombre" },
                values: new object[] { new Guid("0b6a3c1e-5d0e-4f0a-9a52-6d7a1f0f3a01"), true, new DateTime(2026, 11, 11, 0, 0, 0, 0, DateTimeKind.Utc), "San Martín" });

            migrationBuilder.InsertData(
                table: "TiposVehiculo",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[,]
                {
                    { new Guid("6dece58d-f153-4c45-94cf-7c8b138a4879"), true, "Moto" },
                    { new Guid("f8184fbd-3989-40c0-973a-0cc5bab89dab"), true, "Auto" }
                });

            migrationBuilder.InsertData(
                table: "Servicios",
                columns: new[] { "Id", "Activo", "AplicaLado", "AplicaPosicion", "CategoriaVehiculo", "Nombre", "PrecioReferencia", "TallerId" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-4000-8000-000000000001"), true, false, true, "Auto", "Alineación", 32000m, new Guid("0b6a3c1e-5d0e-4f0a-9a52-6d7a1f0f3a01") },
                    { new Guid("a1000000-0000-4000-8000-000000000002"), true, true, true, "Ambos", "Balanceo", 18000m, new Guid("0b6a3c1e-5d0e-4f0a-9a52-6d7a1f0f3a01") },
                    { new Guid("a1000000-0000-4000-8000-000000000003"), true, true, true, "Ambos", "Reparación de neumático", 12000m, new Guid("0b6a3c1e-5d0e-4f0a-9a52-6d7a1f0f3a01") },
                    { new Guid("a1000000-0000-4000-8000-000000000004"), true, true, true, "Ambos", "Cambio de neumáticos", null, new Guid("0b6a3c1e-5d0e-4f0a-9a52-6d7a1f0f3a01") },
                    { new Guid("a1000000-0000-4000-8000-000000000005"), true, false, false, "Auto", "Rotación", null, new Guid("0b6a3c1e-5d0e-4f0a-9a52-6d7a1f0f3a01") },
                    { new Guid("a1000000-0000-4000-8000-000000000006"), true, false, false, "Moto", "Service de moto", null, new Guid("0b6a3c1e-5d0e-4f0a-9a52-6d7a1f0f3a01") },
                    { new Guid("a1000000-0000-4000-8000-000000000007"), true, false, false, "Ambos", "Mecánica general", null, new Guid("0b6a3c1e-5d0e-4f0a-9a52-6d7a1f0f3a01") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_TallerId_Estado",
                table: "OrdenesTrabajo",
                columns: new[] { "TallerId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_UsuarioId",
                table: "OrdenesTrabajo",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_VehiculoId",
                table: "OrdenesTrabajo",
                column: "VehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoServicio_OrdenTrabajoId",
                table: "OrdenesTrabajoServicio",
                column: "OrdenTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoServicio_ServicioId",
                table: "OrdenesTrabajoServicio",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajoServicio_TallerId",
                table: "OrdenesTrabajoServicio",
                column: "TallerId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TallerId",
                table: "Productos",
                column: "TallerId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_TallerId",
                table: "Servicios",
                column: "TallerId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TallerId_Email",
                table: "Usuarios",
                columns: new[] { "TallerId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_TallerId_Patente",
                table: "Vehiculos",
                columns: new[] { "TallerId", "Patente" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_TipoVehiculoId",
                table: "Vehiculos",
                column: "TipoVehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ProductoId",
                table: "Ventas",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_TallerId",
                table: "Ventas",
                column: "TallerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdenesTrabajoServicio");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "OrdenesTrabajo");

            migrationBuilder.DropTable(
                name: "Servicios");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Vehiculos");

            migrationBuilder.DropTable(
                name: "Talleres");

            migrationBuilder.DropTable(
                name: "TiposVehiculo");
        }
    }
}
