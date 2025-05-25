using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioPromedio = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    File_FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    File_SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    File_ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Divisas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Simbolo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Movimientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroMovimiento = table.Column<int>(type: "integer", nullable: false),
                    Broker = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    FechaConcertacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaLiquidacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Comision = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IvaComision = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OtrosImpuestos = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Moneda = table.Column<string>(type: "text", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movimientos_DataPoints_DataPointId",
                        column: x => x.DataPointId,
                        principalTable: "DataPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activos_Ticker",
                table: "Activos",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activos_Tipo",
                table: "Activos",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_DataPoints_CreatedAt",
                table: "DataPoints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataPoints_Status",
                table: "DataPoints",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Divisas_Codigo",
                table: "Divisas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Divisas_Tipo",
                table: "Divisas",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_Broker",
                table: "Movimientos",
                column: "Broker");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_DataPointId",
                table: "Movimientos",
                column: "DataPointId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_FechaConcertacion",
                table: "Movimientos",
                column: "FechaConcertacion");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_NumeroMovimiento",
                table: "Movimientos",
                column: "NumeroMovimiento");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_Ticker",
                table: "Movimientos",
                column: "Ticker");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_Tipo",
                table: "Movimientos",
                column: "Tipo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activos");

            migrationBuilder.DropTable(
                name: "Divisas");

            migrationBuilder.DropTable(
                name: "Movimientos");

            migrationBuilder.DropTable(
                name: "DataPoints");
        }
    }
}
