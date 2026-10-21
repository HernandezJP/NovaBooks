using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TikalLibros.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentTermsAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PED_ComentarioAprobacion",
                table: "PB_PEDIDO_CLIENTE",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PED_EmpleadoAprobadorId",
                table: "PB_PEDIDO_CLIENTE",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PED_FechaAprobacion",
                table: "PB_PEDIDO_CLIENTE",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PED_MotivoCancelacion",
                table: "PB_PEDIDO_CLIENTE",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PED_TipoVentaId",
                table: "PB_PEDIDO_CLIENTE",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_EmpleadoAprobadorId",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_EmpleadoAprobadorId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_TipoVentaId",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_TipoVentaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PB_PEDIDO_APROBACION",
                table: "PB_PEDIDO_CLIENTE",
                sql: "([PED_EmpleadoAprobadorId] IS NULL AND [PED_FechaAprobacion] IS NULL) OR ([PED_EmpleadoAprobadorId] IS NOT NULL AND [PED_FechaAprobacion] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_PB_PEDIDO_CLIENTE_PB_EMPLEADO_PED_EmpleadoAprobadorId",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_EmpleadoAprobadorId",
                principalTable: "PB_EMPLEADO",
                principalColumn: "EMP_Empleado",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PB_PEDIDO_CLIENTE_PB_TIPO_VENTA_PED_TipoVentaId",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_TipoVentaId",
                principalTable: "PB_TIPO_VENTA",
                principalColumn: "TVE_TipoVenta",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PB_PEDIDO_CLIENTE_PB_EMPLEADO_PED_EmpleadoAprobadorId",
                table: "PB_PEDIDO_CLIENTE");

            migrationBuilder.DropForeignKey(
                name: "FK_PB_PEDIDO_CLIENTE_PB_TIPO_VENTA_PED_TipoVentaId",
                table: "PB_PEDIDO_CLIENTE");

            migrationBuilder.DropIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_EmpleadoAprobadorId",
                table: "PB_PEDIDO_CLIENTE");

            migrationBuilder.DropIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_TipoVentaId",
                table: "PB_PEDIDO_CLIENTE");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PB_PEDIDO_APROBACION",
                table: "PB_PEDIDO_CLIENTE");

            migrationBuilder.DropColumn(
                name: "PED_ComentarioAprobacion",
                table: "PB_PEDIDO_CLIENTE");

            migrationBuilder.DropColumn(
                name: "PED_EmpleadoAprobadorId",
                table: "PB_PEDIDO_CLIENTE");

            migrationBuilder.DropColumn(
                name: "PED_FechaAprobacion",
                table: "PB_PEDIDO_CLIENTE");

            migrationBuilder.DropColumn(
                name: "PED_MotivoCancelacion",
                table: "PB_PEDIDO_CLIENTE");

            migrationBuilder.DropColumn(
                name: "PED_TipoVentaId",
                table: "PB_PEDIDO_CLIENTE");
        }
    }
}
