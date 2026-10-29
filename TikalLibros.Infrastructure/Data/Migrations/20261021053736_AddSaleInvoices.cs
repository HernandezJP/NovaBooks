using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TikalLibros.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PB_FACTURA_VENTA",
                columns: table => new
                {
                    FAC_FacturaVenta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FAC_VentaId = table.Column<int>(type: "int", nullable: false),
                    FAC_NumeroAutorizacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FAC_Serie = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    FAC_Numero = table.Column<long>(type: "bigint", nullable: false),
                    FAC_FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FAC_FechaCertificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FAC_NitEmisor = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FAC_NitReceptor = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FAC_NombreReceptor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    FAC_Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FAC_Impuesto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FAC_EsDemostracion = table.Column<bool>(type: "bit", nullable: false),
                    FAC_Anulada = table.Column<bool>(type: "bit", nullable: false),
                    FAC_FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FAC_MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FAC_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_FACTURA_VENTA", x => x.FAC_FacturaVenta);
                    table.CheckConstraint("CK_PB_FACTURA_VENTA_ANULACION", "[FAC_Anulada] = 0 OR ([FAC_FechaAnulacion] IS NOT NULL AND [FAC_MotivoAnulacion] IS NOT NULL)");
                    table.CheckConstraint("CK_PB_FACTURA_VENTA_MONTOS", "[FAC_Total] >= 0 AND [FAC_Impuesto] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_FACTURA_VENTA_PB_VENTA_FAC_VentaId",
                        column: x => x.FAC_VentaId,
                        principalTable: "PB_VENTA",
                        principalColumn: "VEN_Venta",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PB_FACTURA_VENTA_FAC_NumeroAutorizacion",
                table: "PB_FACTURA_VENTA",
                column: "FAC_NumeroAutorizacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_FACTURA_VENTA_FAC_Serie_FAC_Numero",
                table: "PB_FACTURA_VENTA",
                columns: new[] { "FAC_Serie", "FAC_Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_FACTURA_VENTA_FAC_VentaId",
                table: "PB_FACTURA_VENTA",
                column: "FAC_VentaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PB_FACTURA_VENTA");
        }
    }
}
