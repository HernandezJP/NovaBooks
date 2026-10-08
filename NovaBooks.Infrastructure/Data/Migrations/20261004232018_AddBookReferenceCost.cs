using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaBooks.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookReferenceCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LIB_CostoReferencia",
                table: "PB_LIBRO",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PB_LIBRO_COSTO_REFERENCIA",
                table: "PB_LIBRO",
                sql: "[LIB_CostoReferencia] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PB_LIBRO_COSTO_REFERENCIA",
                table: "PB_LIBRO");

            migrationBuilder.DropColumn(
                name: "LIB_CostoReferencia",
                table: "PB_LIBRO");
        }
    }
}
