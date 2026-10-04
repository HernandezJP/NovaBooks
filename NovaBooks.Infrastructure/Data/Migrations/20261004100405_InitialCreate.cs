using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaBooks.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GEO_PAIS",
                columns: table => new
                {
                    PAI_Pais = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PAI_Codigo = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    PAI_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PAI_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GEO_PAIS", x => x.PAI_Pais);
                });

            migrationBuilder.CreateTable(
                name: "PB_AUTOR",
                columns: table => new
                {
                    AUT_Autor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AUT_PrimerNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AUT_SegundoNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AUT_PrimerApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AUT_SegundoApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AUT_Seudonimo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AUT_FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    AUT_FechaFallecimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    AUT_Biografia = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AUT_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AUT_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AUT_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_AUTOR", x => x.AUT_Autor);
                    table.CheckConstraint("CK_PB_AUTOR_FECHAS", "[AUT_FechaFallecimiento] IS NULL OR [AUT_FechaNacimiento] IS NULL OR [AUT_FechaFallecimiento] >= [AUT_FechaNacimiento]");
                });

            migrationBuilder.CreateTable(
                name: "PB_CATEGORIA",
                columns: table => new
                {
                    CAT_Categoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CAT_CategoriaPadreId = table.Column<int>(type: "int", nullable: true),
                    CAT_Codigo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    CAT_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CAT_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CAT_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CAT_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CAT_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_CATEGORIA", x => x.CAT_Categoria);
                    table.ForeignKey(
                        name: "FK_PB_CATEGORIA_PB_CATEGORIA_CAT_CategoriaPadreId",
                        column: x => x.CAT_CategoriaPadreId,
                        principalTable: "PB_CATEGORIA",
                        principalColumn: "CAT_Categoria",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_EDITORIAL",
                columns: table => new
                {
                    EDI_Editorial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EDI_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EDI_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EDI_SitioWeb = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EDI_Correo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EDI_Telefono = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: true),
                    EDI_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EDI_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EDI_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_EDITORIAL", x => x.EDI_Editorial);
                });

            migrationBuilder.CreateTable(
                name: "PB_ESTADO_DEVOLUCION",
                columns: table => new
                {
                    EDV_EstadoDevolucion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EDV_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EDV_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EDV_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EDV_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_ESTADO_DEVOLUCION", x => x.EDV_EstadoDevolucion);
                });

            migrationBuilder.CreateTable(
                name: "PB_ESTADO_ORDEN_COMPRA",
                columns: table => new
                {
                    EOC_EstadoOrdenCompra = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EOC_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EOC_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EOC_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EOC_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_ESTADO_ORDEN_COMPRA", x => x.EOC_EstadoOrdenCompra);
                });

            migrationBuilder.CreateTable(
                name: "PB_ESTADO_PEDIDO",
                columns: table => new
                {
                    EPE_EstadoPedido = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EPE_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EPE_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EPE_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EPE_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_ESTADO_PEDIDO", x => x.EPE_EstadoPedido);
                });

            migrationBuilder.CreateTable(
                name: "PB_ESTADO_RECEPCION_COMPRA",
                columns: table => new
                {
                    ERC_EstadoRecepcionCompra = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ERC_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ERC_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ERC_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ERC_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_ESTADO_RECEPCION_COMPRA", x => x.ERC_EstadoRecepcionCompra);
                });

            migrationBuilder.CreateTable(
                name: "PB_ESTADO_VENTA",
                columns: table => new
                {
                    EVE_EstadoVenta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EVE_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EVE_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EVE_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EVE_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_ESTADO_VENTA", x => x.EVE_EstadoVenta);
                });

            migrationBuilder.CreateTable(
                name: "PB_FORMATO_LIBRO",
                columns: table => new
                {
                    FLI_FormatoLibro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FLI_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FLI_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FLI_Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FLI_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_FORMATO_LIBRO", x => x.FLI_FormatoLibro);
                });

            migrationBuilder.CreateTable(
                name: "PB_IDIOMA",
                columns: table => new
                {
                    IDI_Idioma = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IDI_Codigo = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    IDI_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IDI_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_IDIOMA", x => x.IDI_Idioma);
                });

            migrationBuilder.CreateTable(
                name: "PB_IMPUESTO",
                columns: table => new
                {
                    IMP_Impuesto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IMP_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    IMP_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IMP_Porcentaje = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IMP_FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    IMP_FechaFin = table.Column<DateOnly>(type: "date", nullable: true),
                    IMP_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IMP_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IMP_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_IMPUESTO", x => x.IMP_Impuesto);
                    table.CheckConstraint("CK_PB_IMPUESTO_FECHAS", "[IMP_FechaFin] IS NULL OR [IMP_FechaFin] >= [IMP_FechaInicio]");
                    table.CheckConstraint("CK_PB_IMPUESTO_PORCENTAJE", "[IMP_Porcentaje] >= 0 AND [IMP_Porcentaje] <= 100");
                });

            migrationBuilder.CreateTable(
                name: "PB_LISTA_PRECIO",
                columns: table => new
                {
                    LPR_ListaPrecio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LPR_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    LPR_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LPR_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LPR_EsPredeterminada = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LPR_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LPR_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LPR_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_LISTA_PRECIO", x => x.LPR_ListaPrecio);
                });

            migrationBuilder.CreateTable(
                name: "PB_METODO_PAGO",
                columns: table => new
                {
                    MPA_MetodoPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MPA_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    MPA_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MPA_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MPA_RequiereReferencia = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MPA_RequiereAutorizacion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MPA_AfectaEfectivo = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MPA_PermiteCambio = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MPA_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MPA_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    MPA_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_METODO_PAGO", x => x.MPA_MetodoPago);
                });

            migrationBuilder.CreateTable(
                name: "PB_MONEDA",
                columns: table => new
                {
                    MON_Moneda = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MON_Codigo = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    MON_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MON_Simbolo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MON_Decimales = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    MON_EsPredeterminada = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MON_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_MONEDA", x => x.MON_Moneda);
                    table.CheckConstraint("CK_PB_MONEDA_DECIMALES", "[MON_Decimales] >= 0 AND [MON_Decimales] <= 6");
                });

            migrationBuilder.CreateTable(
                name: "PB_MOTIVO_AJUSTE",
                columns: table => new
                {
                    MAJ_MotivoAjuste = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MAJ_Codigo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    MAJ_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MAJ_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MAJ_Naturaleza = table.Column<short>(type: "smallint", nullable: false),
                    MAJ_RequiereObservacion = table.Column<bool>(type: "bit", nullable: false),
                    MAJ_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_MOTIVO_AJUSTE", x => x.MAJ_MotivoAjuste);
                    table.CheckConstraint("CK_PB_MOTIVO_AJUSTE_NATURALEZA", "[MAJ_Naturaleza] IN (-1, 1)");
                });

            migrationBuilder.CreateTable(
                name: "PB_MOTIVO_DEVOLUCION",
                columns: table => new
                {
                    MDV_MotivoDevolucion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MDV_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    MDV_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MDV_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MDV_ReintegraInventario = table.Column<bool>(type: "bit", nullable: false),
                    MDV_RequiereObservacion = table.Column<bool>(type: "bit", nullable: false),
                    MDV_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_MOTIVO_DEVOLUCION", x => x.MDV_MotivoDevolucion);
                });

            migrationBuilder.CreateTable(
                name: "PB_PUESTO",
                columns: table => new
                {
                    PUE_Puesto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PUE_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PUE_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PUE_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PUE_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PUE_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PUESTO", x => x.PUE_Puesto);
                });

            migrationBuilder.CreateTable(
                name: "PB_TIPO_DIRECCION",
                columns: table => new
                {
                    TDI_TipoDireccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TDI_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TDI_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TDI_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_TIPO_DIRECCION", x => x.TDI_TipoDireccion);
                });

            migrationBuilder.CreateTable(
                name: "PB_TIPO_IDENTIFICACION",
                columns: table => new
                {
                    TID_TipoIdentificacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TID_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TID_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TID_LongitudMinima = table.Column<int>(type: "int", nullable: true),
                    TID_LongitudMaxima = table.Column<int>(type: "int", nullable: true),
                    TID_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_TIPO_IDENTIFICACION", x => x.TID_TipoIdentificacion);
                    table.CheckConstraint("CK_PB_TIPO_IDENTIFICACION_LONGITUD", "[TID_LongitudMinima] IS NULL OR [TID_LongitudMaxima] IS NULL OR [TID_LongitudMaxima] >= [TID_LongitudMinima]");
                });

            migrationBuilder.CreateTable(
                name: "PB_TIPO_MOVIMIENTO_CAJA",
                columns: table => new
                {
                    TMC_TipoMovimientoCaja = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TMC_Codigo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    TMC_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TMC_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TMC_Naturaleza = table.Column<short>(type: "smallint", nullable: false),
                    TMC_RequiereAutorizacion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TMC_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_TIPO_MOVIMIENTO_CAJA", x => x.TMC_TipoMovimientoCaja);
                    table.CheckConstraint("CK_PB_TIPO_MOVIMIENTO_CAJA_NATURALEZA", "[TMC_Naturaleza] IN (-1, 1)");
                });

            migrationBuilder.CreateTable(
                name: "PB_TIPO_MOVIMIENTO_INVENTARIO",
                columns: table => new
                {
                    TMI_TipoMovimientoInventario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TMI_Codigo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    TMI_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TMI_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TMI_Naturaleza = table.Column<short>(type: "smallint", nullable: false),
                    TMI_RequiereAlmacenOrigen = table.Column<bool>(type: "bit", nullable: false),
                    TMI_RequiereAlmacenDestino = table.Column<bool>(type: "bit", nullable: false),
                    TMI_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_TIPO_MOVIMIENTO_INVENTARIO", x => x.TMI_TipoMovimientoInventario);
                    table.CheckConstraint("CK_PB_TIPO_MOVIMIENTO_NATURALEZA", "[TMI_Naturaleza] IN (-1, 0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "PB_TIPO_PERSONA",
                columns: table => new
                {
                    TPR_TipoPersona = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TPR_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TPR_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TPR_Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TPR_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_TIPO_PERSONA", x => x.TPR_TipoPersona);
                });

            migrationBuilder.CreateTable(
                name: "PB_TIPO_TELEFONO",
                columns: table => new
                {
                    TTE_TipoTelefono = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TTE_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TTE_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TTE_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_TIPO_TELEFONO", x => x.TTE_TipoTelefono);
                });

            migrationBuilder.CreateTable(
                name: "PB_TIPO_VENTA",
                columns: table => new
                {
                    TVE_TipoVenta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TVE_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TVE_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TVE_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TVE_RequiereCliente = table.Column<bool>(type: "bit", nullable: false),
                    TVE_GeneraCuentaPorCobrar = table.Column<bool>(type: "bit", nullable: false),
                    TVE_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_TIPO_VENTA", x => x.TVE_TipoVenta);
                });

            migrationBuilder.CreateTable(
                name: "SEG_ROL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ROL_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ROL_EsSistema = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ROL_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ROL_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ROL_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEG_ROL", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GEO_DEPARTAMENTO",
                columns: table => new
                {
                    DEP_Departamento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DEP_PaisId = table.Column<int>(type: "int", nullable: false),
                    DEP_Codigo = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    DEP_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DEP_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GEO_DEPARTAMENTO", x => x.DEP_Departamento);
                    table.ForeignKey(
                        name: "FK_GEO_DEPARTAMENTO_GEO_PAIS_DEP_PaisId",
                        column: x => x.DEP_PaisId,
                        principalTable: "GEO_PAIS",
                        principalColumn: "PAI_Pais",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_LIBRO",
                columns: table => new
                {
                    LIB_Libro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LIB_Codigo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    LIB_ISBN10 = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    LIB_ISBN13 = table.Column<string>(type: "varchar(13)", unicode: false, maxLength: 13, nullable: true),
                    LIB_CodigoBarras = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    LIB_Titulo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LIB_Subtitulo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LIB_EditorialId = table.Column<int>(type: "int", nullable: true),
                    LIB_IdiomaId = table.Column<int>(type: "int", nullable: false),
                    LIB_FormatoLibroId = table.Column<int>(type: "int", nullable: false),
                    LIB_ImpuestoId = table.Column<int>(type: "int", nullable: false),
                    LIB_NumeroEdicion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LIB_AnioPublicacion = table.Column<int>(type: "int", nullable: true),
                    LIB_NumeroPaginas = table.Column<int>(type: "int", nullable: true),
                    LIB_AltoCentimetros = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LIB_AnchoCentimetros = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LIB_GrosorCentimetros = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LIB_PesoGramos = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LIB_Descripcion = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LIB_RutaImagen = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LIB_PermiteVenta = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LIB_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LIB_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LIB_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_LIBRO", x => x.LIB_Libro);
                    table.CheckConstraint("CK_PB_LIBRO_ANIO_PUBLICACION", "[LIB_AnioPublicacion] IS NULL OR [LIB_AnioPublicacion] BETWEEN 1000 AND 9999");
                    table.CheckConstraint("CK_PB_LIBRO_DIMENSIONES", "([LIB_AltoCentimetros] IS NULL OR [LIB_AltoCentimetros] > 0) AND ([LIB_AnchoCentimetros] IS NULL OR [LIB_AnchoCentimetros] > 0) AND ([LIB_GrosorCentimetros] IS NULL OR [LIB_GrosorCentimetros] > 0) AND ([LIB_PesoGramos] IS NULL OR [LIB_PesoGramos] > 0)");
                    table.CheckConstraint("CK_PB_LIBRO_NUMERO_PAGINAS", "[LIB_NumeroPaginas] IS NULL OR [LIB_NumeroPaginas] > 0");
                    table.ForeignKey(
                        name: "FK_PB_LIBRO_PB_EDITORIAL_LIB_EditorialId",
                        column: x => x.LIB_EditorialId,
                        principalTable: "PB_EDITORIAL",
                        principalColumn: "EDI_Editorial",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_LIBRO_PB_FORMATO_LIBRO_LIB_FormatoLibroId",
                        column: x => x.LIB_FormatoLibroId,
                        principalTable: "PB_FORMATO_LIBRO",
                        principalColumn: "FLI_FormatoLibro",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_LIBRO_PB_IDIOMA_LIB_IdiomaId",
                        column: x => x.LIB_IdiomaId,
                        principalTable: "PB_IDIOMA",
                        principalColumn: "IDI_Idioma",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_LIBRO_PB_IMPUESTO_LIB_ImpuestoId",
                        column: x => x.LIB_ImpuestoId,
                        principalTable: "PB_IMPUESTO",
                        principalColumn: "IMP_Impuesto",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PERSONA",
                columns: table => new
                {
                    PER_Persona = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PER_TipoPersonaId = table.Column<int>(type: "int", nullable: false),
                    PER_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PER_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PER_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PERSONA", x => x.PER_Persona);
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_PB_TIPO_PERSONA_PER_TipoPersonaId",
                        column: x => x.PER_TipoPersonaId,
                        principalTable: "PB_TIPO_PERSONA",
                        principalColumn: "TPR_TipoPersona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SEG_ROL_CLAIM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEG_ROL_CLAIM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SEG_ROL_CLAIM_SEG_ROL_RoleId",
                        column: x => x.RoleId,
                        principalTable: "SEG_ROL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GEO_MUNICIPIO",
                columns: table => new
                {
                    MUN_Municipio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MUN_DepartamentoId = table.Column<int>(type: "int", nullable: false),
                    MUN_Codigo = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    MUN_Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MUN_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GEO_MUNICIPIO", x => x.MUN_Municipio);
                    table.ForeignKey(
                        name: "FK_GEO_MUNICIPIO_GEO_DEPARTAMENTO_MUN_DepartamentoId",
                        column: x => x.MUN_DepartamentoId,
                        principalTable: "GEO_DEPARTAMENTO",
                        principalColumn: "DEP_Departamento",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_LIBRO_AUTOR",
                columns: table => new
                {
                    LAU_LibroId = table.Column<int>(type: "int", nullable: false),
                    LAU_AutorId = table.Column<int>(type: "int", nullable: false),
                    LAU_Orden = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    LAU_TipoParticipacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_LIBRO_AUTOR", x => new { x.LAU_LibroId, x.LAU_AutorId });
                    table.CheckConstraint("CK_PB_LIBRO_AUTOR_ORDEN", "[LAU_Orden] > 0");
                    table.ForeignKey(
                        name: "FK_PB_LIBRO_AUTOR_PB_AUTOR_LAU_AutorId",
                        column: x => x.LAU_AutorId,
                        principalTable: "PB_AUTOR",
                        principalColumn: "AUT_Autor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_LIBRO_AUTOR_PB_LIBRO_LAU_LibroId",
                        column: x => x.LAU_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_LIBRO_CATEGORIA",
                columns: table => new
                {
                    LCA_LibroId = table.Column<int>(type: "int", nullable: false),
                    LCA_CategoriaId = table.Column<int>(type: "int", nullable: false),
                    LCA_Principal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_LIBRO_CATEGORIA", x => new { x.LCA_LibroId, x.LCA_CategoriaId });
                    table.ForeignKey(
                        name: "FK_PB_LIBRO_CATEGORIA_PB_CATEGORIA_LCA_CategoriaId",
                        column: x => x.LCA_CategoriaId,
                        principalTable: "PB_CATEGORIA",
                        principalColumn: "CAT_Categoria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_LIBRO_CATEGORIA_PB_LIBRO_LCA_LibroId",
                        column: x => x.LCA_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_LIBRO_PRECIO",
                columns: table => new
                {
                    LIP_LibroPrecio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LIP_LibroId = table.Column<int>(type: "int", nullable: false),
                    LIP_ListaPrecioId = table.Column<int>(type: "int", nullable: false),
                    LIP_Precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LIP_FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LIP_FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LIP_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LIP_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_LIBRO_PRECIO", x => x.LIP_LibroPrecio);
                    table.CheckConstraint("CK_PB_LIBRO_PRECIO_FECHAS", "[LIP_FechaFin] IS NULL OR [LIP_FechaFin] >= [LIP_FechaInicio]");
                    table.CheckConstraint("CK_PB_LIBRO_PRECIO_PRECIO", "[LIP_Precio] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_LIBRO_PRECIO_PB_LIBRO_LIP_LibroId",
                        column: x => x.LIP_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_LIBRO_PRECIO_PB_LISTA_PRECIO_LIP_ListaPrecioId",
                        column: x => x.LIP_ListaPrecioId,
                        principalTable: "PB_LISTA_PRECIO",
                        principalColumn: "LPR_ListaPrecio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_CLIENTE",
                columns: table => new
                {
                    CLI_Cliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CLI_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CLI_PersonaId = table.Column<int>(type: "int", nullable: false),
                    CLI_LimiteCredito = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CLI_DiasCredito = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CLI_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CLI_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CLI_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CLI_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_CLIENTE", x => x.CLI_Cliente);
                    table.CheckConstraint("CK_PB_CLIENTE_DIAS_CREDITO", "[CLI_DiasCredito] >= 0");
                    table.CheckConstraint("CK_PB_CLIENTE_LIMITE_CREDITO", "[CLI_LimiteCredito] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_CLIENTE_PB_PERSONA_CLI_PersonaId",
                        column: x => x.CLI_PersonaId,
                        principalTable: "PB_PERSONA",
                        principalColumn: "PER_Persona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_EMPRESA",
                columns: table => new
                {
                    EMP_Empresa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EMP_PersonaId = table.Column<int>(type: "int", nullable: false),
                    EMP_MonedaPredeterminadaId = table.Column<int>(type: "int", nullable: false),
                    EMP_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EMP_NombreComercial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EMP_LogoRuta = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EMP_SitioWeb = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EMP_Correo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EMP_Telefono = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: true),
                    EMP_ZonaHoraria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "America/Guatemala"),
                    EMP_FormatoFecha = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "dd/MM/yyyy"),
                    EMP_DecimalesCantidad = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    EMP_DecimalesPrecio = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    EMP_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EMP_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EMP_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_EMPRESA", x => x.EMP_Empresa);
                    table.CheckConstraint("CK_PB_EMPRESA_DECIMALES_CANTIDAD", "[EMP_DecimalesCantidad] >= 0 AND [EMP_DecimalesCantidad] <= 6");
                    table.CheckConstraint("CK_PB_EMPRESA_DECIMALES_PRECIO", "[EMP_DecimalesPrecio] >= 0 AND [EMP_DecimalesPrecio] <= 6");
                    table.ForeignKey(
                        name: "FK_PB_EMPRESA_PB_MONEDA_EMP_MonedaPredeterminadaId",
                        column: x => x.EMP_MonedaPredeterminadaId,
                        principalTable: "PB_MONEDA",
                        principalColumn: "MON_Moneda",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_EMPRESA_PB_PERSONA_EMP_PersonaId",
                        column: x => x.EMP_PersonaId,
                        principalTable: "PB_PERSONA",
                        principalColumn: "PER_Persona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PERSONA_CORREO",
                columns: table => new
                {
                    PCO_PersonaCorreo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PCO_PersonaId = table.Column<int>(type: "int", nullable: false),
                    PCO_Correo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PCO_Principal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PCO_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PERSONA_CORREO", x => x.PCO_PersonaCorreo);
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_CORREO_PB_PERSONA_PCO_PersonaId",
                        column: x => x.PCO_PersonaId,
                        principalTable: "PB_PERSONA",
                        principalColumn: "PER_Persona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PERSONA_IDENTIFICACION",
                columns: table => new
                {
                    PID_PersonaIdentificacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PID_PersonaId = table.Column<int>(type: "int", nullable: false),
                    PID_TipoIdentificacionId = table.Column<int>(type: "int", nullable: false),
                    PID_Numero = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    PID_FechaEmision = table.Column<DateOnly>(type: "date", nullable: true),
                    PID_FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    PID_Principal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PID_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PERSONA_IDENTIFICACION", x => x.PID_PersonaIdentificacion);
                    table.CheckConstraint("CK_PB_PERSONA_IDENTIFICACION_FECHAS", "[PID_FechaVencimiento] IS NULL OR [PID_FechaEmision] IS NULL OR [PID_FechaVencimiento] >= [PID_FechaEmision]");
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_IDENTIFICACION_PB_PERSONA_PID_PersonaId",
                        column: x => x.PID_PersonaId,
                        principalTable: "PB_PERSONA",
                        principalColumn: "PER_Persona",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_IDENTIFICACION_PB_TIPO_IDENTIFICACION_PID_TipoIdentificacionId",
                        column: x => x.PID_TipoIdentificacionId,
                        principalTable: "PB_TIPO_IDENTIFICACION",
                        principalColumn: "TID_TipoIdentificacion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PERSONA_JURIDICA",
                columns: table => new
                {
                    PJU_PersonaId = table.Column<int>(type: "int", nullable: false),
                    PJU_RazonSocial = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PJU_NombreComercial = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PJU_FechaConstitucion = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PERSONA_JURIDICA", x => x.PJU_PersonaId);
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_JURIDICA_PB_PERSONA_PJU_PersonaId",
                        column: x => x.PJU_PersonaId,
                        principalTable: "PB_PERSONA",
                        principalColumn: "PER_Persona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PERSONA_NATURAL",
                columns: table => new
                {
                    PNA_PersonaId = table.Column<int>(type: "int", nullable: false),
                    PNA_PrimerNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PNA_SegundoNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PNA_TercerNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PNA_PrimerApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PNA_SegundoApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PNA_ApellidoCasada = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PNA_FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PERSONA_NATURAL", x => x.PNA_PersonaId);
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_NATURAL_PB_PERSONA_PNA_PersonaId",
                        column: x => x.PNA_PersonaId,
                        principalTable: "PB_PERSONA",
                        principalColumn: "PER_Persona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PERSONA_TELEFONO",
                columns: table => new
                {
                    PTE_PersonaTelefono = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTE_PersonaId = table.Column<int>(type: "int", nullable: false),
                    PTE_TipoTelefonoId = table.Column<int>(type: "int", nullable: false),
                    PTE_Numero = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: false),
                    PTE_Extension = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    PTE_Principal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PTE_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PERSONA_TELEFONO", x => x.PTE_PersonaTelefono);
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_TELEFONO_PB_PERSONA_PTE_PersonaId",
                        column: x => x.PTE_PersonaId,
                        principalTable: "PB_PERSONA",
                        principalColumn: "PER_Persona",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_TELEFONO_PB_TIPO_TELEFONO_PTE_TipoTelefonoId",
                        column: x => x.PTE_TipoTelefonoId,
                        principalTable: "PB_TIPO_TELEFONO",
                        principalColumn: "TTE_TipoTelefono",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PROVEEDOR",
                columns: table => new
                {
                    PRO_Proveedor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PRO_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PRO_PersonaId = table.Column<int>(type: "int", nullable: false),
                    PRO_LimiteCredito = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    PRO_DiasCredito = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PRO_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PRO_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PRO_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PRO_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PROVEEDOR", x => x.PRO_Proveedor);
                    table.CheckConstraint("CK_PB_PROVEEDOR_DIAS_CREDITO", "[PRO_DiasCredito] >= 0");
                    table.CheckConstraint("CK_PB_PROVEEDOR_LIMITE_CREDITO", "[PRO_LimiteCredito] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_PROVEEDOR_PB_PERSONA_PRO_PersonaId",
                        column: x => x.PRO_PersonaId,
                        principalTable: "PB_PERSONA",
                        principalColumn: "PER_Persona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_DIRECCION",
                columns: table => new
                {
                    DIR_Direccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DIR_MunicipioId = table.Column<int>(type: "int", nullable: false),
                    DIR_Zona = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DIR_Linea1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DIR_Linea2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DIR_CodigoPostal = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    DIR_Referencia = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DIR_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DIR_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DIR_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_DIRECCION", x => x.DIR_Direccion);
                    table.ForeignKey(
                        name: "FK_PB_DIRECCION_GEO_MUNICIPIO_DIR_MunicipioId",
                        column: x => x.DIR_MunicipioId,
                        principalTable: "GEO_MUNICIPIO",
                        principalColumn: "MUN_Municipio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PARAMETRO_SISTEMA",
                columns: table => new
                {
                    PAR_ParametroSistema = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PAR_EmpresaId = table.Column<int>(type: "int", nullable: false),
                    PAR_Clave = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    PAR_Valor = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PAR_TipoDato = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "STRING"),
                    PAR_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PAR_EsEditable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PAR_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PAR_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PAR_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PARAMETRO_SISTEMA", x => x.PAR_ParametroSistema);
                    table.CheckConstraint("CK_PB_PARAMETRO_SISTEMA_TIPO_DATO", "[PAR_TipoDato] IN ('STRING', 'INTEGER', 'DECIMAL', 'BOOLEAN', 'DATE', 'JSON')");
                    table.ForeignKey(
                        name: "FK_PB_PARAMETRO_SISTEMA_PB_EMPRESA_PAR_EmpresaId",
                        column: x => x.PAR_EmpresaId,
                        principalTable: "PB_EMPRESA",
                        principalColumn: "EMP_Empresa",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PERSONA_DIRECCION",
                columns: table => new
                {
                    PDI_PersonaDireccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PDI_PersonaId = table.Column<int>(type: "int", nullable: false),
                    PDI_DireccionId = table.Column<int>(type: "int", nullable: false),
                    PDI_TipoDireccionId = table.Column<int>(type: "int", nullable: false),
                    PDI_Principal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PDI_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PERSONA_DIRECCION", x => x.PDI_PersonaDireccion);
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_DIRECCION_PB_DIRECCION_PDI_DireccionId",
                        column: x => x.PDI_DireccionId,
                        principalTable: "PB_DIRECCION",
                        principalColumn: "DIR_Direccion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_DIRECCION_PB_PERSONA_PDI_PersonaId",
                        column: x => x.PDI_PersonaId,
                        principalTable: "PB_PERSONA",
                        principalColumn: "PER_Persona",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_PERSONA_DIRECCION_PB_TIPO_DIRECCION_PDI_TipoDireccionId",
                        column: x => x.PDI_TipoDireccionId,
                        principalTable: "PB_TIPO_DIRECCION",
                        principalColumn: "TDI_TipoDireccion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_SUCURSAL",
                columns: table => new
                {
                    SUC_Sucursal = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SUC_EmpresaId = table.Column<int>(type: "int", nullable: false),
                    SUC_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SUC_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SUC_DireccionId = table.Column<int>(type: "int", nullable: false),
                    SUC_Telefono = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: true),
                    SUC_Correo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SUC_EsPrincipal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SUC_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SUC_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    SUC_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_SUCURSAL", x => x.SUC_Sucursal);
                    table.ForeignKey(
                        name: "FK_PB_SUCURSAL_PB_DIRECCION_SUC_DireccionId",
                        column: x => x.SUC_DireccionId,
                        principalTable: "PB_DIRECCION",
                        principalColumn: "DIR_Direccion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_SUCURSAL_PB_EMPRESA_SUC_EmpresaId",
                        column: x => x.SUC_EmpresaId,
                        principalTable: "PB_EMPRESA",
                        principalColumn: "EMP_Empresa",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_ALMACEN",
                columns: table => new
                {
                    ALM_Almacen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ALM_SucursalId = table.Column<int>(type: "int", nullable: false),
                    ALM_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ALM_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ALM_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ALM_EsPrincipal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ALM_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ALM_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ALM_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_ALMACEN", x => x.ALM_Almacen);
                    table.ForeignKey(
                        name: "FK_PB_ALMACEN_PB_SUCURSAL_ALM_SucursalId",
                        column: x => x.ALM_SucursalId,
                        principalTable: "PB_SUCURSAL",
                        principalColumn: "SUC_Sucursal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_CAJA",
                columns: table => new
                {
                    CAJ_Caja = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CAJ_SucursalId = table.Column<int>(type: "int", nullable: false),
                    CAJ_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CAJ_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CAJ_Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CAJ_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CAJ_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CAJ_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_CAJA", x => x.CAJ_Caja);
                    table.ForeignKey(
                        name: "FK_PB_CAJA_PB_SUCURSAL_CAJ_SucursalId",
                        column: x => x.CAJ_SucursalId,
                        principalTable: "PB_SUCURSAL",
                        principalColumn: "SUC_Sucursal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_EMPLEADO",
                columns: table => new
                {
                    EMP_Empleado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EMP_Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EMP_PersonaId = table.Column<int>(type: "int", nullable: false),
                    EMP_PuestoId = table.Column<int>(type: "int", nullable: false),
                    EMP_SucursalId = table.Column<int>(type: "int", nullable: false),
                    EMP_FechaContratacion = table.Column<DateOnly>(type: "date", nullable: false),
                    EMP_FechaFinalizacion = table.Column<DateOnly>(type: "date", nullable: true),
                    EMP_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EMP_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EMP_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_EMPLEADO", x => x.EMP_Empleado);
                    table.CheckConstraint("CK_PB_EMPLEADO_FECHAS", "[EMP_FechaFinalizacion] IS NULL OR [EMP_FechaFinalizacion] >= [EMP_FechaContratacion]");
                    table.ForeignKey(
                        name: "FK_PB_EMPLEADO_PB_PERSONA_EMP_PersonaId",
                        column: x => x.EMP_PersonaId,
                        principalTable: "PB_PERSONA",
                        principalColumn: "PER_Persona",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_EMPLEADO_PB_PUESTO_EMP_PuestoId",
                        column: x => x.EMP_PuestoId,
                        principalTable: "PB_PUESTO",
                        principalColumn: "PUE_Puesto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_EMPLEADO_PB_SUCURSAL_EMP_SucursalId",
                        column: x => x.EMP_SucursalId,
                        principalTable: "PB_SUCURSAL",
                        principalColumn: "SUC_Sucursal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_SECUENCIA_DOCUMENTO",
                columns: table => new
                {
                    SEC_SecuenciaDocumento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SEC_EmpresaId = table.Column<int>(type: "int", nullable: false),
                    SEC_SucursalId = table.Column<int>(type: "int", nullable: true),
                    SEC_TipoDocumento = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SEC_Serie = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SEC_Prefijo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    SEC_Sufijo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    SEC_UltimoNumero = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    SEC_LongitudNumero = table.Column<int>(type: "int", nullable: false, defaultValue: 8),
                    SEC_ReiniciaAnualmente = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SEC_UltimoAnio = table.Column<int>(type: "int", nullable: true),
                    SEC_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SEC_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    SEC_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SEC_Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_SECUENCIA_DOCUMENTO", x => x.SEC_SecuenciaDocumento);
                    table.CheckConstraint("CK_PB_SECUENCIA_LONGITUD", "[SEC_LongitudNumero] >= 1 AND [SEC_LongitudNumero] <= 20");
                    table.CheckConstraint("CK_PB_SECUENCIA_ULTIMO_ANIO", "[SEC_UltimoAnio] IS NULL OR [SEC_UltimoAnio] BETWEEN 2000 AND 9999");
                    table.CheckConstraint("CK_PB_SECUENCIA_ULTIMO_NUMERO", "[SEC_UltimoNumero] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_SECUENCIA_DOCUMENTO_PB_EMPRESA_SEC_EmpresaId",
                        column: x => x.SEC_EmpresaId,
                        principalTable: "PB_EMPRESA",
                        principalColumn: "EMP_Empresa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_SECUENCIA_DOCUMENTO_PB_SUCURSAL_SEC_SucursalId",
                        column: x => x.SEC_SucursalId,
                        principalTable: "PB_SUCURSAL",
                        principalColumn: "SUC_Sucursal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_EXISTENCIA",
                columns: table => new
                {
                    EXI_Existencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EXI_AlmacenId = table.Column<int>(type: "int", nullable: false),
                    EXI_LibroId = table.Column<int>(type: "int", nullable: false),
                    EXI_CantidadDisponible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    EXI_CantidadReservada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    EXI_StockMinimo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    EXI_StockMaximo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    EXI_Ubicacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EXI_FechaUltimoMovimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EXI_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EXI_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EXI_Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_EXISTENCIA", x => x.EXI_Existencia);
                    table.CheckConstraint("CK_PB_EXISTENCIA_CANTIDADES", "[EXI_CantidadDisponible] >= 0 AND [EXI_CantidadReservada] >= 0 AND [EXI_CantidadReservada] <= [EXI_CantidadDisponible]");
                    table.CheckConstraint("CK_PB_EXISTENCIA_STOCK", "[EXI_StockMinimo] >= 0 AND [EXI_StockMaximo] >= 0 AND ([EXI_StockMaximo] = 0 OR [EXI_StockMaximo] >= [EXI_StockMinimo])");
                    table.ForeignKey(
                        name: "FK_PB_EXISTENCIA_PB_ALMACEN_EXI_AlmacenId",
                        column: x => x.EXI_AlmacenId,
                        principalTable: "PB_ALMACEN",
                        principalColumn: "ALM_Almacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_EXISTENCIA_PB_LIBRO_EXI_LibroId",
                        column: x => x.EXI_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_LOTE",
                columns: table => new
                {
                    LOT_Lote = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LOT_AlmacenId = table.Column<int>(type: "int", nullable: false),
                    LOT_LibroId = table.Column<int>(type: "int", nullable: false),
                    LOT_ProveedorId = table.Column<int>(type: "int", nullable: true),
                    LOT_Numero = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    LOT_FechaIngreso = table.Column<DateOnly>(type: "date", nullable: false),
                    LOT_FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    LOT_CantidadInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LOT_CantidadActual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LOT_CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LOT_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LOT_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LOT_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_LOTE", x => x.LOT_Lote);
                    table.CheckConstraint("CK_PB_LOTE_CANTIDADES", "[LOT_CantidadInicial] > 0 AND [LOT_CantidadActual] >= 0 AND [LOT_CantidadActual] <= [LOT_CantidadInicial]");
                    table.CheckConstraint("CK_PB_LOTE_COSTO", "[LOT_CostoUnitario] >= 0");
                    table.CheckConstraint("CK_PB_LOTE_FECHAS", "[LOT_FechaVencimiento] IS NULL OR [LOT_FechaVencimiento] >= [LOT_FechaIngreso]");
                    table.ForeignKey(
                        name: "FK_PB_LOTE_PB_ALMACEN_LOT_AlmacenId",
                        column: x => x.LOT_AlmacenId,
                        principalTable: "PB_ALMACEN",
                        principalColumn: "ALM_Almacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_LOTE_PB_LIBRO_LOT_LibroId",
                        column: x => x.LOT_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_LOTE_PB_PROVEEDOR_LOT_ProveedorId",
                        column: x => x.LOT_ProveedorId,
                        principalTable: "PB_PROVEEDOR",
                        principalColumn: "PRO_Proveedor",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_APERTURA_CAJA",
                columns: table => new
                {
                    ACA_AperturaCaja = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ACA_CajaId = table.Column<int>(type: "int", nullable: false),
                    ACA_EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    ACA_MonedaId = table.Column<int>(type: "int", nullable: false),
                    ACA_FechaApertura = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ACA_MontoInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ACA_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ACA_Abierta = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ACA_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_APERTURA_CAJA", x => x.ACA_AperturaCaja);
                    table.CheckConstraint("CK_PB_APERTURA_CAJA_MONTO", "[ACA_MontoInicial] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_APERTURA_CAJA_PB_CAJA_ACA_CajaId",
                        column: x => x.ACA_CajaId,
                        principalTable: "PB_CAJA",
                        principalColumn: "CAJ_Caja",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_APERTURA_CAJA_PB_EMPLEADO_ACA_EmpleadoId",
                        column: x => x.ACA_EmpleadoId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_APERTURA_CAJA_PB_MONEDA_ACA_MonedaId",
                        column: x => x.ACA_MonedaId,
                        principalTable: "PB_MONEDA",
                        principalColumn: "MON_Moneda",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_ORDEN_COMPRA",
                columns: table => new
                {
                    OCO_OrdenCompra = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OCO_Numero = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    OCO_ProveedorId = table.Column<int>(type: "int", nullable: false),
                    OCO_SucursalId = table.Column<int>(type: "int", nullable: false),
                    OCO_MonedaId = table.Column<int>(type: "int", nullable: false),
                    OCO_EstadoOrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    OCO_EmpleadoSolicitanteId = table.Column<int>(type: "int", nullable: true),
                    OCO_FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    OCO_FechaEntregaEsperada = table.Column<DateOnly>(type: "date", nullable: true),
                    OCO_TipoCambio = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1m),
                    OCO_Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OCO_Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OCO_Impuesto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OCO_Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OCO_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OCO_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OCO_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    OCO_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_ORDEN_COMPRA", x => x.OCO_OrdenCompra);
                    table.CheckConstraint("CK_PB_ORDEN_COMPRA_DESCUENTO", "[OCO_Descuento] <= [OCO_Subtotal]");
                    table.CheckConstraint("CK_PB_ORDEN_COMPRA_FECHA_ENTREGA", "[OCO_FechaEntregaEsperada] IS NULL OR [OCO_FechaEntregaEsperada] >= CAST([OCO_FechaEmision] AS date)");
                    table.CheckConstraint("CK_PB_ORDEN_COMPRA_MONTOS", "[OCO_Subtotal] >= 0 AND [OCO_Descuento] >= 0 AND [OCO_Impuesto] >= 0 AND [OCO_Total] >= 0");
                    table.CheckConstraint("CK_PB_ORDEN_COMPRA_TIPO_CAMBIO", "[OCO_TipoCambio] > 0");
                    table.ForeignKey(
                        name: "FK_PB_ORDEN_COMPRA_PB_EMPLEADO_OCO_EmpleadoSolicitanteId",
                        column: x => x.OCO_EmpleadoSolicitanteId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_ORDEN_COMPRA_PB_ESTADO_ORDEN_COMPRA_OCO_EstadoOrdenCompraId",
                        column: x => x.OCO_EstadoOrdenCompraId,
                        principalTable: "PB_ESTADO_ORDEN_COMPRA",
                        principalColumn: "EOC_EstadoOrdenCompra",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_ORDEN_COMPRA_PB_MONEDA_OCO_MonedaId",
                        column: x => x.OCO_MonedaId,
                        principalTable: "PB_MONEDA",
                        principalColumn: "MON_Moneda",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_ORDEN_COMPRA_PB_PROVEEDOR_OCO_ProveedorId",
                        column: x => x.OCO_ProveedorId,
                        principalTable: "PB_PROVEEDOR",
                        principalColumn: "PRO_Proveedor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_ORDEN_COMPRA_PB_SUCURSAL_OCO_SucursalId",
                        column: x => x.OCO_SucursalId,
                        principalTable: "PB_SUCURSAL",
                        principalColumn: "SUC_Sucursal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PEDIDO_CLIENTE",
                columns: table => new
                {
                    PED_PedidoCliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PED_Numero = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    PED_ClienteId = table.Column<int>(type: "int", nullable: false),
                    PED_SucursalId = table.Column<int>(type: "int", nullable: false),
                    PED_AlmacenId = table.Column<int>(type: "int", nullable: false),
                    PED_MonedaId = table.Column<int>(type: "int", nullable: false),
                    PED_EstadoPedidoId = table.Column<int>(type: "int", nullable: false),
                    PED_EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    PED_Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PED_FechaVencimientoReserva = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PED_TipoCambio = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1m),
                    PED_Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PED_Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PED_Impuesto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PED_Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PED_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PED_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PED_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PED_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PEDIDO_CLIENTE", x => x.PED_PedidoCliente);
                    table.CheckConstraint("CK_PB_PEDIDO_FECHAS", "[PED_FechaVencimientoReserva] IS NULL OR [PED_FechaVencimientoReserva] >= [PED_Fecha]");
                    table.CheckConstraint("CK_PB_PEDIDO_MONTOS", "[PED_Subtotal] >= 0 AND [PED_Descuento] >= 0 AND [PED_Impuesto] >= 0 AND [PED_Total] >= 0");
                    table.CheckConstraint("CK_PB_PEDIDO_TIPO_CAMBIO", "[PED_TipoCambio] > 0");
                    table.ForeignKey(
                        name: "FK_PB_PEDIDO_CLIENTE_PB_ALMACEN_PED_AlmacenId",
                        column: x => x.PED_AlmacenId,
                        principalTable: "PB_ALMACEN",
                        principalColumn: "ALM_Almacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_PEDIDO_CLIENTE_PB_CLIENTE_PED_ClienteId",
                        column: x => x.PED_ClienteId,
                        principalTable: "PB_CLIENTE",
                        principalColumn: "CLI_Cliente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_PEDIDO_CLIENTE_PB_EMPLEADO_PED_EmpleadoId",
                        column: x => x.PED_EmpleadoId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_PEDIDO_CLIENTE_PB_ESTADO_PEDIDO_PED_EstadoPedidoId",
                        column: x => x.PED_EstadoPedidoId,
                        principalTable: "PB_ESTADO_PEDIDO",
                        principalColumn: "EPE_EstadoPedido",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_PEDIDO_CLIENTE_PB_MONEDA_PED_MonedaId",
                        column: x => x.PED_MonedaId,
                        principalTable: "PB_MONEDA",
                        principalColumn: "MON_Moneda",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_PEDIDO_CLIENTE_PB_SUCURSAL_PED_SucursalId",
                        column: x => x.PED_SucursalId,
                        principalTable: "PB_SUCURSAL",
                        principalColumn: "SUC_Sucursal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SEG_USUARIO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    USU_EmpleadoId = table.Column<int>(type: "int", nullable: true),
                    USU_Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    USU_DebeCambiarPassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    USU_FechaUltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    USU_FechaCambioPassword = table.Column<DateTime>(type: "datetime2", nullable: true),
                    USU_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    USU_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    USU_CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    USU_ModificadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEG_USUARIO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SEG_USUARIO_PB_EMPLEADO_USU_EmpleadoId",
                        column: x => x.USU_EmpleadoId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SEG_USUARIO_SEG_USUARIO_USU_CreadoPorUsuarioId",
                        column: x => x.USU_CreadoPorUsuarioId,
                        principalTable: "SEG_USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SEG_USUARIO_SEG_USUARIO_USU_ModificadoPorUsuarioId",
                        column: x => x.USU_ModificadoPorUsuarioId,
                        principalTable: "SEG_USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_CIERRE_CAJA",
                columns: table => new
                {
                    CCA_CierreCaja = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CCA_AperturaCajaId = table.Column<int>(type: "int", nullable: false),
                    CCA_EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    CCA_FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CCA_MontoInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CCA_TotalIngresosEfectivo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CCA_TotalEgresosEfectivo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CCA_MontoEsperado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CCA_MontoContado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CCA_Diferencia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CCA_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CCA_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_CIERRE_CAJA", x => x.CCA_CierreCaja);
                    table.CheckConstraint("CK_PB_CIERRE_CAJA_DIFERENCIA", "[CCA_Diferencia] = [CCA_MontoContado] - [CCA_MontoEsperado]");
                    table.CheckConstraint("CK_PB_CIERRE_CAJA_MONTOS", "[CCA_MontoInicial] >= 0 AND [CCA_TotalIngresosEfectivo] >= 0 AND [CCA_TotalEgresosEfectivo] >= 0 AND [CCA_MontoEsperado] >= 0 AND [CCA_MontoContado] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_CIERRE_CAJA_PB_APERTURA_CAJA_CCA_AperturaCajaId",
                        column: x => x.CCA_AperturaCajaId,
                        principalTable: "PB_APERTURA_CAJA",
                        principalColumn: "ACA_AperturaCaja",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_CIERRE_CAJA_PB_EMPLEADO_CCA_EmpleadoId",
                        column: x => x.CCA_EmpleadoId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_ORDEN_COMPRA_DETALLE",
                columns: table => new
                {
                    OCD_OrdenCompraDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OCD_OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    OCD_LibroId = table.Column<int>(type: "int", nullable: false),
                    OCD_CantidadSolicitada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OCD_CantidadRecibida = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OCD_CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OCD_PorcentajeDescuento = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OCD_MontoDescuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OCD_PorcentajeImpuesto = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OCD_MontoImpuesto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OCD_Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OCD_Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OCD_Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_ORDEN_COMPRA_DETALLE", x => x.OCD_OrdenCompraDetalle);
                    table.CheckConstraint("CK_PB_ORDEN_COMPRA_DETALLE_CANTIDADES", "[OCD_CantidadSolicitada] > 0 AND [OCD_CantidadRecibida] >= 0 AND [OCD_CantidadRecibida] <= [OCD_CantidadSolicitada]");
                    table.CheckConstraint("CK_PB_ORDEN_COMPRA_DETALLE_COSTO", "[OCD_CostoUnitario] >= 0");
                    table.CheckConstraint("CK_PB_ORDEN_COMPRA_DETALLE_DESCUENTO", "[OCD_PorcentajeDescuento] >= 0 AND [OCD_PorcentajeDescuento] <= 100 AND [OCD_MontoDescuento] >= 0");
                    table.CheckConstraint("CK_PB_ORDEN_COMPRA_DETALLE_IMPUESTO", "[OCD_PorcentajeImpuesto] >= 0 AND [OCD_PorcentajeImpuesto] <= 100 AND [OCD_MontoImpuesto] >= 0");
                    table.CheckConstraint("CK_PB_ORDEN_COMPRA_DETALLE_TOTALES", "[OCD_Subtotal] >= 0 AND [OCD_Total] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_ORDEN_COMPRA_DETALLE_PB_LIBRO_OCD_LibroId",
                        column: x => x.OCD_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_ORDEN_COMPRA_DETALLE_PB_ORDEN_COMPRA_OCD_OrdenCompraId",
                        column: x => x.OCD_OrdenCompraId,
                        principalTable: "PB_ORDEN_COMPRA",
                        principalColumn: "OCO_OrdenCompra",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_RECEPCION_COMPRA",
                columns: table => new
                {
                    REC_RecepcionCompra = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    REC_Numero = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    REC_OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    REC_AlmacenId = table.Column<int>(type: "int", nullable: false),
                    REC_EstadoRecepcionCompraId = table.Column<int>(type: "int", nullable: false),
                    REC_EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    REC_FechaRecepcion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    REC_NumeroDocumentoProveedor = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    REC_SerieDocumentoProveedor = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    REC_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    REC_ActualizaInventario = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    REC_FechaProcesado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    REC_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    REC_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_RECEPCION_COMPRA", x => x.REC_RecepcionCompra);
                    table.ForeignKey(
                        name: "FK_PB_RECEPCION_COMPRA_PB_ALMACEN_REC_AlmacenId",
                        column: x => x.REC_AlmacenId,
                        principalTable: "PB_ALMACEN",
                        principalColumn: "ALM_Almacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_RECEPCION_COMPRA_PB_EMPLEADO_REC_EmpleadoId",
                        column: x => x.REC_EmpleadoId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_RECEPCION_COMPRA_PB_ESTADO_RECEPCION_COMPRA_REC_EstadoRecepcionCompraId",
                        column: x => x.REC_EstadoRecepcionCompraId,
                        principalTable: "PB_ESTADO_RECEPCION_COMPRA",
                        principalColumn: "ERC_EstadoRecepcionCompra",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_RECEPCION_COMPRA_PB_ORDEN_COMPRA_REC_OrdenCompraId",
                        column: x => x.REC_OrdenCompraId,
                        principalTable: "PB_ORDEN_COMPRA",
                        principalColumn: "OCO_OrdenCompra",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_PEDIDO_CLIENTE_DETALLE",
                columns: table => new
                {
                    PDD_PedidoClienteDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PDD_PedidoClienteId = table.Column<int>(type: "int", nullable: false),
                    PDD_LibroId = table.Column<int>(type: "int", nullable: false),
                    PDD_NumeroLinea = table.Column<int>(type: "int", nullable: false),
                    PDD_Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PDD_CantidadReservada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PDD_CantidadFacturada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PDD_PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PDD_PorcentajeDescuento = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PDD_MontoDescuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PDD_PorcentajeImpuesto = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PDD_MontoImpuesto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PDD_Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PDD_Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_PEDIDO_CLIENTE_DETALLE", x => x.PDD_PedidoClienteDetalle);
                    table.CheckConstraint("CK_PB_PEDIDO_DETALLE_CANTIDADES", "[PDD_Cantidad] > 0 AND [PDD_CantidadReservada] >= 0 AND [PDD_CantidadFacturada] >= 0 AND [PDD_CantidadReservada] <= [PDD_Cantidad] AND [PDD_CantidadFacturada] <= [PDD_Cantidad]");
                    table.CheckConstraint("CK_PB_PEDIDO_DETALLE_MONTOS", "[PDD_PrecioUnitario] >= 0 AND [PDD_MontoDescuento] >= 0 AND [PDD_MontoImpuesto] >= 0 AND [PDD_Subtotal] >= 0 AND [PDD_Total] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_PEDIDO_CLIENTE_DETALLE_PB_LIBRO_PDD_LibroId",
                        column: x => x.PDD_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_PEDIDO_CLIENTE_DETALLE_PB_PEDIDO_CLIENTE_PDD_PedidoClienteId",
                        column: x => x.PDD_PedidoClienteId,
                        principalTable: "PB_PEDIDO_CLIENTE",
                        principalColumn: "PED_PedidoCliente",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_VENTA",
                columns: table => new
                {
                    VEN_Venta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VEN_Numero = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    VEN_PedidoClienteId = table.Column<int>(type: "int", nullable: true),
                    VEN_ClienteId = table.Column<int>(type: "int", nullable: true),
                    VEN_SucursalId = table.Column<int>(type: "int", nullable: false),
                    VEN_AlmacenId = table.Column<int>(type: "int", nullable: false),
                    VEN_EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    VEN_AperturaCajaId = table.Column<int>(type: "int", nullable: true),
                    VEN_TipoVentaId = table.Column<int>(type: "int", nullable: false),
                    VEN_EstadoVentaId = table.Column<int>(type: "int", nullable: false),
                    VEN_MonedaId = table.Column<int>(type: "int", nullable: false),
                    VEN_Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    VEN_TipoCambio = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1m),
                    VEN_Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VEN_Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VEN_Impuesto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VEN_Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VEN_TotalPagado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VEN_CambioEntregado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VEN_FechaVencimientoCredito = table.Column<DateOnly>(type: "date", nullable: true),
                    VEN_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VEN_InventarioProcesado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VEN_FechaProcesado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VEN_Anulada = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VEN_FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VEN_MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VEN_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    VEN_FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_VENTA", x => x.VEN_Venta);
                    table.CheckConstraint("CK_PB_VENTA_ANULACION", "[VEN_Anulada] = 0 OR ([VEN_FechaAnulacion] IS NOT NULL AND [VEN_MotivoAnulacion] IS NOT NULL)");
                    table.CheckConstraint("CK_PB_VENTA_MONTOS", "[VEN_Subtotal] >= 0 AND [VEN_Descuento] >= 0 AND [VEN_Impuesto] >= 0 AND [VEN_Total] >= 0 AND [VEN_TotalPagado] >= 0 AND [VEN_CambioEntregado] >= 0");
                    table.CheckConstraint("CK_PB_VENTA_TIPO_CAMBIO", "[VEN_TipoCambio] > 0");
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PB_ALMACEN_VEN_AlmacenId",
                        column: x => x.VEN_AlmacenId,
                        principalTable: "PB_ALMACEN",
                        principalColumn: "ALM_Almacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PB_APERTURA_CAJA_VEN_AperturaCajaId",
                        column: x => x.VEN_AperturaCajaId,
                        principalTable: "PB_APERTURA_CAJA",
                        principalColumn: "ACA_AperturaCaja",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PB_CLIENTE_VEN_ClienteId",
                        column: x => x.VEN_ClienteId,
                        principalTable: "PB_CLIENTE",
                        principalColumn: "CLI_Cliente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PB_EMPLEADO_VEN_EmpleadoId",
                        column: x => x.VEN_EmpleadoId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PB_ESTADO_VENTA_VEN_EstadoVentaId",
                        column: x => x.VEN_EstadoVentaId,
                        principalTable: "PB_ESTADO_VENTA",
                        principalColumn: "EVE_EstadoVenta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PB_MONEDA_VEN_MonedaId",
                        column: x => x.VEN_MonedaId,
                        principalTable: "PB_MONEDA",
                        principalColumn: "MON_Moneda",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PB_PEDIDO_CLIENTE_VEN_PedidoClienteId",
                        column: x => x.VEN_PedidoClienteId,
                        principalTable: "PB_PEDIDO_CLIENTE",
                        principalColumn: "PED_PedidoCliente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PB_SUCURSAL_VEN_SucursalId",
                        column: x => x.VEN_SucursalId,
                        principalTable: "PB_SUCURSAL",
                        principalColumn: "SUC_Sucursal",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PB_TIPO_VENTA_VEN_TipoVentaId",
                        column: x => x.VEN_TipoVentaId,
                        principalTable: "PB_TIPO_VENTA",
                        principalColumn: "TVE_TipoVenta",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_AUDITORIA",
                columns: table => new
                {
                    AUD_Auditoria = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AUD_UsuarioId = table.Column<int>(type: "int", nullable: true),
                    AUD_Accion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    AUD_Entidad = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    AUD_EntidadId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    AUD_Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AUD_DireccionIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    AUD_UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AUD_Ruta = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AUD_MetodoHttp = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    AUD_CorrelacionId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    AUD_Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_AUDITORIA", x => x.AUD_Auditoria);
                    table.ForeignKey(
                        name: "FK_PB_AUDITORIA_SEG_USUARIO_AUD_UsuarioId",
                        column: x => x.AUD_UsuarioId,
                        principalTable: "SEG_USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_HISTORIAL_ACCESO",
                columns: table => new
                {
                    HAC_HistorialAcceso = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HAC_UsuarioId = table.Column<int>(type: "int", nullable: true),
                    HAC_NombreUsuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    HAC_Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    HAC_Exitoso = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HAC_MotivoFallo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HAC_DireccionIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    HAC_UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HAC_CorrelacionId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_HISTORIAL_ACCESO", x => x.HAC_HistorialAcceso);
                    table.ForeignKey(
                        name: "FK_PB_HISTORIAL_ACCESO_SEG_USUARIO_HAC_UsuarioId",
                        column: x => x.HAC_UsuarioId,
                        principalTable: "SEG_USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SEG_USUARIO_CLAIM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEG_USUARIO_CLAIM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SEG_USUARIO_CLAIM_SEG_USUARIO_UserId",
                        column: x => x.UserId,
                        principalTable: "SEG_USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SEG_USUARIO_LOGIN",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEG_USUARIO_LOGIN", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_SEG_USUARIO_LOGIN_SEG_USUARIO_UserId",
                        column: x => x.UserId,
                        principalTable: "SEG_USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SEG_USUARIO_ROL",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEG_USUARIO_ROL", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_SEG_USUARIO_ROL_SEG_ROL_RoleId",
                        column: x => x.RoleId,
                        principalTable: "SEG_ROL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SEG_USUARIO_ROL_SEG_USUARIO_UserId",
                        column: x => x.UserId,
                        principalTable: "SEG_USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SEG_USUARIO_TOKEN",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEG_USUARIO_TOKEN", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_SEG_USUARIO_TOKEN_SEG_USUARIO_UserId",
                        column: x => x.UserId,
                        principalTable: "SEG_USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PB_CIERRE_CAJA_DETALLE",
                columns: table => new
                {
                    CCD_CierreCajaDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CCD_CierreCajaId = table.Column<int>(type: "int", nullable: false),
                    CCD_MetodoPagoId = table.Column<int>(type: "int", nullable: false),
                    CCD_MontoSistema = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CCD_MontoContado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CCD_Diferencia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CCD_Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_CIERRE_CAJA_DETALLE", x => x.CCD_CierreCajaDetalle);
                    table.CheckConstraint("CK_PB_CIERRE_DETALLE_DIFERENCIA", "[CCD_Diferencia] = [CCD_MontoContado] - [CCD_MontoSistema]");
                    table.CheckConstraint("CK_PB_CIERRE_DETALLE_MONTOS", "[CCD_MontoSistema] >= 0 AND [CCD_MontoContado] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_CIERRE_CAJA_DETALLE_PB_CIERRE_CAJA_CCD_CierreCajaId",
                        column: x => x.CCD_CierreCajaId,
                        principalTable: "PB_CIERRE_CAJA",
                        principalColumn: "CCA_CierreCaja",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_CIERRE_CAJA_DETALLE_PB_METODO_PAGO_CCD_MetodoPagoId",
                        column: x => x.CCD_MetodoPagoId,
                        principalTable: "PB_METODO_PAGO",
                        principalColumn: "MPA_MetodoPago",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_MOVIMIENTO_INVENTARIO",
                columns: table => new
                {
                    MOV_MovimientoInventario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MOV_Numero = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    MOV_TipoMovimientoId = table.Column<int>(type: "int", nullable: false),
                    MOV_AlmacenOrigenId = table.Column<int>(type: "int", nullable: true),
                    MOV_AlmacenDestinoId = table.Column<int>(type: "int", nullable: true),
                    MOV_RecepcionCompraId = table.Column<int>(type: "int", nullable: true),
                    MOV_EmpleadoId = table.Column<int>(type: "int", nullable: true),
                    MOV_Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    MOV_DocumentoReferencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MOV_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MOV_Procesado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MOV_FechaProcesado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MOV_Anulado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MOV_FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MOV_MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MOV_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_MOVIMIENTO_INVENTARIO", x => x.MOV_MovimientoInventario);
                    table.CheckConstraint("CK_PB_MOVIMIENTO_ALMACENES", "[MOV_AlmacenOrigenId] IS NOT NULL OR [MOV_AlmacenDestinoId] IS NOT NULL");
                    table.CheckConstraint("CK_PB_MOVIMIENTO_ALMACENES_DIFERENTES", "[MOV_AlmacenOrigenId] IS NULL OR [MOV_AlmacenDestinoId] IS NULL OR [MOV_AlmacenOrigenId] <> [MOV_AlmacenDestinoId]");
                    table.CheckConstraint("CK_PB_MOVIMIENTO_ANULACION", "[MOV_Anulado] = 0 OR ([MOV_FechaAnulacion] IS NOT NULL AND [MOV_MotivoAnulacion] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_INVENTARIO_PB_ALMACEN_MOV_AlmacenDestinoId",
                        column: x => x.MOV_AlmacenDestinoId,
                        principalTable: "PB_ALMACEN",
                        principalColumn: "ALM_Almacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_INVENTARIO_PB_ALMACEN_MOV_AlmacenOrigenId",
                        column: x => x.MOV_AlmacenOrigenId,
                        principalTable: "PB_ALMACEN",
                        principalColumn: "ALM_Almacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_INVENTARIO_PB_EMPLEADO_MOV_EmpleadoId",
                        column: x => x.MOV_EmpleadoId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_INVENTARIO_PB_RECEPCION_COMPRA_MOV_RecepcionCompraId",
                        column: x => x.MOV_RecepcionCompraId,
                        principalTable: "PB_RECEPCION_COMPRA",
                        principalColumn: "REC_RecepcionCompra",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_INVENTARIO_PB_TIPO_MOVIMIENTO_INVENTARIO_MOV_TipoMovimientoId",
                        column: x => x.MOV_TipoMovimientoId,
                        principalTable: "PB_TIPO_MOVIMIENTO_INVENTARIO",
                        principalColumn: "TMI_TipoMovimientoInventario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_RECEPCION_COMPRA_DETALLE",
                columns: table => new
                {
                    RCD_RecepcionCompraDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RCD_RecepcionCompraId = table.Column<int>(type: "int", nullable: false),
                    RCD_OrdenCompraDetalleId = table.Column<int>(type: "int", nullable: false),
                    RCD_LibroId = table.Column<int>(type: "int", nullable: false),
                    RCD_LoteId = table.Column<int>(type: "int", nullable: true),
                    RCD_CantidadRecibida = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RCD_CantidadAceptada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RCD_CantidadRechazada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RCD_CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RCD_MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RCD_Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_RECEPCION_COMPRA_DETALLE", x => x.RCD_RecepcionCompraDetalle);
                    table.CheckConstraint("CK_PB_RECEPCION_DETALLE_CANTIDADES", "[RCD_CantidadRecibida] > 0 AND [RCD_CantidadAceptada] >= 0 AND [RCD_CantidadRechazada] >= 0 AND [RCD_CantidadRecibida] = [RCD_CantidadAceptada] + [RCD_CantidadRechazada]");
                    table.CheckConstraint("CK_PB_RECEPCION_DETALLE_COSTO", "[RCD_CostoUnitario] >= 0");
                    table.CheckConstraint("CK_PB_RECEPCION_DETALLE_RECHAZO", "[RCD_CantidadRechazada] = 0 OR ([RCD_MotivoRechazo] IS NOT NULL AND LEN(LTRIM(RTRIM([RCD_MotivoRechazo]))) > 0)");
                    table.ForeignKey(
                        name: "FK_PB_RECEPCION_COMPRA_DETALLE_PB_LIBRO_RCD_LibroId",
                        column: x => x.RCD_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_RECEPCION_COMPRA_DETALLE_PB_LOTE_RCD_LoteId",
                        column: x => x.RCD_LoteId,
                        principalTable: "PB_LOTE",
                        principalColumn: "LOT_Lote",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_RECEPCION_COMPRA_DETALLE_PB_ORDEN_COMPRA_DETALLE_RCD_OrdenCompraDetalleId",
                        column: x => x.RCD_OrdenCompraDetalleId,
                        principalTable: "PB_ORDEN_COMPRA_DETALLE",
                        principalColumn: "OCD_OrdenCompraDetalle",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_RECEPCION_COMPRA_DETALLE_PB_RECEPCION_COMPRA_RCD_RecepcionCompraId",
                        column: x => x.RCD_RecepcionCompraId,
                        principalTable: "PB_RECEPCION_COMPRA",
                        principalColumn: "REC_RecepcionCompra",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_DEVOLUCION_VENTA",
                columns: table => new
                {
                    DEV_DevolucionVenta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DEV_Numero = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    DEV_VentaId = table.Column<int>(type: "int", nullable: false),
                    DEV_EstadoDevolucionId = table.Column<int>(type: "int", nullable: false),
                    DEV_AlmacenId = table.Column<int>(type: "int", nullable: false),
                    DEV_EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    DEV_AperturaCajaId = table.Column<int>(type: "int", nullable: true),
                    DEV_MonedaId = table.Column<int>(type: "int", nullable: false),
                    DEV_Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DEV_TipoCambio = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DEV_Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DEV_Impuesto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DEV_Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DEV_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DEV_InventarioProcesado = table.Column<bool>(type: "bit", nullable: false),
                    DEV_ReembolsoProcesado = table.Column<bool>(type: "bit", nullable: false),
                    DEV_FechaProcesado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DEV_Anulada = table.Column<bool>(type: "bit", nullable: false),
                    DEV_FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DEV_MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DEV_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_DEVOLUCION_VENTA", x => x.DEV_DevolucionVenta);
                    table.CheckConstraint("CK_PB_DEVOLUCION_MONTOS", "[DEV_TipoCambio] > 0 AND [DEV_Subtotal] >= 0 AND [DEV_Impuesto] >= 0 AND [DEV_Total] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_VENTA_PB_ALMACEN_DEV_AlmacenId",
                        column: x => x.DEV_AlmacenId,
                        principalTable: "PB_ALMACEN",
                        principalColumn: "ALM_Almacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_VENTA_PB_APERTURA_CAJA_DEV_AperturaCajaId",
                        column: x => x.DEV_AperturaCajaId,
                        principalTable: "PB_APERTURA_CAJA",
                        principalColumn: "ACA_AperturaCaja",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_VENTA_PB_EMPLEADO_DEV_EmpleadoId",
                        column: x => x.DEV_EmpleadoId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_VENTA_PB_ESTADO_DEVOLUCION_DEV_EstadoDevolucionId",
                        column: x => x.DEV_EstadoDevolucionId,
                        principalTable: "PB_ESTADO_DEVOLUCION",
                        principalColumn: "EDV_EstadoDevolucion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_VENTA_PB_MONEDA_DEV_MonedaId",
                        column: x => x.DEV_MonedaId,
                        principalTable: "PB_MONEDA",
                        principalColumn: "MON_Moneda",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_VENTA_PB_VENTA_DEV_VentaId",
                        column: x => x.DEV_VentaId,
                        principalTable: "PB_VENTA",
                        principalColumn: "VEN_Venta",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_VENTA_DETALLE",
                columns: table => new
                {
                    VDE_VentaDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VDE_VentaId = table.Column<int>(type: "int", nullable: false),
                    VDE_LibroId = table.Column<int>(type: "int", nullable: false),
                    VDE_NumeroLinea = table.Column<int>(type: "int", nullable: false),
                    VDE_Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VDE_PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VDE_CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VDE_PorcentajeDescuento = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    VDE_MontoDescuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VDE_PorcentajeImpuesto = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    VDE_MontoImpuesto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VDE_Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VDE_Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VDE_CantidadDevuelta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_VENTA_DETALLE", x => x.VDE_VentaDetalle);
                    table.CheckConstraint("CK_PB_VENTA_DETALLE_CANTIDAD", "[VDE_Cantidad] > 0 AND [VDE_CantidadDevuelta] >= 0 AND [VDE_CantidadDevuelta] <= [VDE_Cantidad]");
                    table.CheckConstraint("CK_PB_VENTA_DETALLE_MONTOS", "[VDE_PrecioUnitario] >= 0 AND [VDE_CostoUnitario] >= 0 AND [VDE_MontoDescuento] >= 0 AND [VDE_MontoImpuesto] >= 0 AND [VDE_Subtotal] >= 0 AND [VDE_Total] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_VENTA_DETALLE_PB_LIBRO_VDE_LibroId",
                        column: x => x.VDE_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_DETALLE_PB_VENTA_VDE_VentaId",
                        column: x => x.VDE_VentaId,
                        principalTable: "PB_VENTA",
                        principalColumn: "VEN_Venta",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_AUDITORIA_DETALLE",
                columns: table => new
                {
                    ADE_AuditoriaDetalle = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ADE_AuditoriaId = table.Column<long>(type: "bigint", nullable: false),
                    ADE_Campo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ADE_ValorAnterior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ADE_ValorNuevo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_AUDITORIA_DETALLE", x => x.ADE_AuditoriaDetalle);
                    table.ForeignKey(
                        name: "FK_PB_AUDITORIA_DETALLE_PB_AUDITORIA_ADE_AuditoriaId",
                        column: x => x.ADE_AuditoriaId,
                        principalTable: "PB_AUDITORIA",
                        principalColumn: "AUD_Auditoria",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PB_AJUSTE_INVENTARIO",
                columns: table => new
                {
                    AJU_AjusteInventario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AJU_Numero = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    AJU_AlmacenId = table.Column<int>(type: "int", nullable: false),
                    AJU_MotivoAjusteId = table.Column<int>(type: "int", nullable: false),
                    AJU_MovimientoInventarioId = table.Column<int>(type: "int", nullable: true),
                    AJU_EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    AJU_Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AJU_Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AJU_Procesado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AJU_FechaProcesado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AJU_Anulado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AJU_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_AJUSTE_INVENTARIO", x => x.AJU_AjusteInventario);
                    table.ForeignKey(
                        name: "FK_PB_AJUSTE_INVENTARIO_PB_ALMACEN_AJU_AlmacenId",
                        column: x => x.AJU_AlmacenId,
                        principalTable: "PB_ALMACEN",
                        principalColumn: "ALM_Almacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_AJUSTE_INVENTARIO_PB_EMPLEADO_AJU_EmpleadoId",
                        column: x => x.AJU_EmpleadoId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_AJUSTE_INVENTARIO_PB_MOTIVO_AJUSTE_AJU_MotivoAjusteId",
                        column: x => x.AJU_MotivoAjusteId,
                        principalTable: "PB_MOTIVO_AJUSTE",
                        principalColumn: "MAJ_MotivoAjuste",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_AJUSTE_INVENTARIO_PB_MOVIMIENTO_INVENTARIO_AJU_MovimientoInventarioId",
                        column: x => x.AJU_MovimientoInventarioId,
                        principalTable: "PB_MOVIMIENTO_INVENTARIO",
                        principalColumn: "MOV_MovimientoInventario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_MOVIMIENTO_INVENTARIO_DETALLE",
                columns: table => new
                {
                    MDE_MovimientoInventarioDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MDE_MovimientoInventarioId = table.Column<int>(type: "int", nullable: false),
                    MDE_LibroId = table.Column<int>(type: "int", nullable: false),
                    MDE_LoteId = table.Column<int>(type: "int", nullable: true),
                    MDE_Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MDE_CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MDE_CostoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MDE_Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_MOVIMIENTO_INVENTARIO_DETALLE", x => x.MDE_MovimientoInventarioDetalle);
                    table.CheckConstraint("CK_PB_MOVIMIENTO_DETALLE_CANTIDAD", "[MDE_Cantidad] > 0");
                    table.CheckConstraint("CK_PB_MOVIMIENTO_DETALLE_COSTOS", "[MDE_CostoUnitario] >= 0 AND [MDE_CostoTotal] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_INVENTARIO_DETALLE_PB_LIBRO_MDE_LibroId",
                        column: x => x.MDE_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_INVENTARIO_DETALLE_PB_LOTE_MDE_LoteId",
                        column: x => x.MDE_LoteId,
                        principalTable: "PB_LOTE",
                        principalColumn: "LOT_Lote",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_INVENTARIO_DETALLE_PB_MOVIMIENTO_INVENTARIO_MDE_MovimientoInventarioId",
                        column: x => x.MDE_MovimientoInventarioId,
                        principalTable: "PB_MOVIMIENTO_INVENTARIO",
                        principalColumn: "MOV_MovimientoInventario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_MOVIMIENTO_CAJA",
                columns: table => new
                {
                    MCA_MovimientoCaja = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MCA_Numero = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    MCA_AperturaCajaId = table.Column<int>(type: "int", nullable: false),
                    MCA_TipoMovimientoCajaId = table.Column<int>(type: "int", nullable: false),
                    MCA_MetodoPagoId = table.Column<int>(type: "int", nullable: false),
                    MCA_MonedaId = table.Column<int>(type: "int", nullable: false),
                    MCA_EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    MCA_VentaId = table.Column<int>(type: "int", nullable: true),
                    MCA_DevolucionVentaId = table.Column<int>(type: "int", nullable: true),
                    MCA_Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    MCA_TipoCambio = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1m),
                    MCA_MontoMoneda = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MCA_MontoBase = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MCA_Referencia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MCA_Concepto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MCA_Anulado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MCA_FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MCA_MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MCA_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_MOVIMIENTO_CAJA", x => x.MCA_MovimientoCaja);
                    table.CheckConstraint("CK_PB_MOVIMIENTO_CAJA_ANULACION", "[MCA_Anulado] = 0 OR ([MCA_FechaAnulacion] IS NOT NULL AND [MCA_MotivoAnulacion] IS NOT NULL)");
                    table.CheckConstraint("CK_PB_MOVIMIENTO_CAJA_MONTOS", "[MCA_TipoCambio] > 0 AND [MCA_MontoMoneda] > 0 AND [MCA_MontoBase] > 0");
                    table.CheckConstraint("CK_PB_MOVIMIENTO_CAJA_REFERENCIA", "NOT ([MCA_VentaId] IS NOT NULL AND [MCA_DevolucionVentaId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_CAJA_PB_APERTURA_CAJA_MCA_AperturaCajaId",
                        column: x => x.MCA_AperturaCajaId,
                        principalTable: "PB_APERTURA_CAJA",
                        principalColumn: "ACA_AperturaCaja",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_CAJA_PB_DEVOLUCION_VENTA_MCA_DevolucionVentaId",
                        column: x => x.MCA_DevolucionVentaId,
                        principalTable: "PB_DEVOLUCION_VENTA",
                        principalColumn: "DEV_DevolucionVenta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_CAJA_PB_EMPLEADO_MCA_EmpleadoId",
                        column: x => x.MCA_EmpleadoId,
                        principalTable: "PB_EMPLEADO",
                        principalColumn: "EMP_Empleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_CAJA_PB_METODO_PAGO_MCA_MetodoPagoId",
                        column: x => x.MCA_MetodoPagoId,
                        principalTable: "PB_METODO_PAGO",
                        principalColumn: "MPA_MetodoPago",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_CAJA_PB_MONEDA_MCA_MonedaId",
                        column: x => x.MCA_MonedaId,
                        principalTable: "PB_MONEDA",
                        principalColumn: "MON_Moneda",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_CAJA_PB_TIPO_MOVIMIENTO_CAJA_MCA_TipoMovimientoCajaId",
                        column: x => x.MCA_TipoMovimientoCajaId,
                        principalTable: "PB_TIPO_MOVIMIENTO_CAJA",
                        principalColumn: "TMC_TipoMovimientoCaja",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_MOVIMIENTO_CAJA_PB_VENTA_MCA_VentaId",
                        column: x => x.MCA_VentaId,
                        principalTable: "PB_VENTA",
                        principalColumn: "VEN_Venta",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_DEVOLUCION_VENTA_DETALLE",
                columns: table => new
                {
                    DVD_DevolucionVentaDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DVD_DevolucionVentaId = table.Column<int>(type: "int", nullable: false),
                    DVD_VentaDetalleId = table.Column<int>(type: "int", nullable: false),
                    DVD_LibroId = table.Column<int>(type: "int", nullable: false),
                    DVD_MotivoDevolucionId = table.Column<int>(type: "int", nullable: false),
                    DVD_Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DVD_PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DVD_MontoImpuesto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DVD_Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DVD_ReintegraInventario = table.Column<bool>(type: "bit", nullable: false),
                    DVD_Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_DEVOLUCION_VENTA_DETALLE", x => x.DVD_DevolucionVentaDetalle);
                    table.CheckConstraint("CK_PB_DEVOLUCION_DETALLE_MONTOS", "[DVD_Cantidad] > 0 AND [DVD_PrecioUnitario] >= 0 AND [DVD_MontoImpuesto] >= 0 AND [DVD_Total] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_VENTA_DETALLE_PB_DEVOLUCION_VENTA_DVD_DevolucionVentaId",
                        column: x => x.DVD_DevolucionVentaId,
                        principalTable: "PB_DEVOLUCION_VENTA",
                        principalColumn: "DEV_DevolucionVenta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_VENTA_DETALLE_PB_LIBRO_DVD_LibroId",
                        column: x => x.DVD_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_VENTA_DETALLE_PB_MOTIVO_DEVOLUCION_DVD_MotivoDevolucionId",
                        column: x => x.DVD_MotivoDevolucionId,
                        principalTable: "PB_MOTIVO_DEVOLUCION",
                        principalColumn: "MDV_MotivoDevolucion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_VENTA_DETALLE_PB_VENTA_DETALLE_DVD_VentaDetalleId",
                        column: x => x.DVD_VentaDetalleId,
                        principalTable: "PB_VENTA_DETALLE",
                        principalColumn: "VDE_VentaDetalle",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_AJUSTE_INVENTARIO_DETALLE",
                columns: table => new
                {
                    AID_AjusteInventarioDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AID_AjusteInventarioId = table.Column<int>(type: "int", nullable: false),
                    AID_LibroId = table.Column<int>(type: "int", nullable: false),
                    AID_CantidadSistema = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AID_CantidadFisica = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AID_Diferencia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AID_CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AID_Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_AJUSTE_INVENTARIO_DETALLE", x => x.AID_AjusteInventarioDetalle);
                    table.CheckConstraint("CK_PB_AJUSTE_DETALLE_CANTIDADES", "[AID_CantidadSistema] >= 0 AND [AID_CantidadFisica] >= 0 AND [AID_Diferencia] = [AID_CantidadFisica] - [AID_CantidadSistema]");
                    table.CheckConstraint("CK_PB_AJUSTE_DETALLE_COSTO", "[AID_CostoUnitario] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_AJUSTE_INVENTARIO_DETALLE_PB_AJUSTE_INVENTARIO_AID_AjusteInventarioId",
                        column: x => x.AID_AjusteInventarioId,
                        principalTable: "PB_AJUSTE_INVENTARIO",
                        principalColumn: "AJU_AjusteInventario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_AJUSTE_INVENTARIO_DETALLE_PB_LIBRO_AID_LibroId",
                        column: x => x.AID_LibroId,
                        principalTable: "PB_LIBRO",
                        principalColumn: "LIB_Libro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_DEVOLUCION_PAGO",
                columns: table => new
                {
                    DPA_DevolucionPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DPA_DevolucionVentaId = table.Column<int>(type: "int", nullable: false),
                    DPA_MetodoPagoId = table.Column<int>(type: "int", nullable: false),
                    DPA_MonedaId = table.Column<int>(type: "int", nullable: false),
                    DPA_MovimientoCajaId = table.Column<int>(type: "int", nullable: true),
                    DPA_Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DPA_TipoCambio = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DPA_MontoMoneda = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DPA_MontoBase = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DPA_Referencia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DPA_Autorizacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DPA_Anulado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_DEVOLUCION_PAGO", x => x.DPA_DevolucionPago);
                    table.CheckConstraint("CK_PB_DEVOLUCION_PAGO_MONTOS", "[DPA_TipoCambio] > 0 AND [DPA_MontoMoneda] > 0 AND [DPA_MontoBase] > 0");
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_PAGO_PB_DEVOLUCION_VENTA_DPA_DevolucionVentaId",
                        column: x => x.DPA_DevolucionVentaId,
                        principalTable: "PB_DEVOLUCION_VENTA",
                        principalColumn: "DEV_DevolucionVenta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_PAGO_PB_METODO_PAGO_DPA_MetodoPagoId",
                        column: x => x.DPA_MetodoPagoId,
                        principalTable: "PB_METODO_PAGO",
                        principalColumn: "MPA_MetodoPago",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_PAGO_PB_MONEDA_DPA_MonedaId",
                        column: x => x.DPA_MonedaId,
                        principalTable: "PB_MONEDA",
                        principalColumn: "MON_Moneda",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_DEVOLUCION_PAGO_PB_MOVIMIENTO_CAJA_DPA_MovimientoCajaId",
                        column: x => x.DPA_MovimientoCajaId,
                        principalTable: "PB_MOVIMIENTO_CAJA",
                        principalColumn: "MCA_MovimientoCaja",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PB_VENTA_PAGO",
                columns: table => new
                {
                    VPA_VentaPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VPA_VentaId = table.Column<int>(type: "int", nullable: false),
                    VPA_MetodoPagoId = table.Column<int>(type: "int", nullable: false),
                    VPA_MonedaId = table.Column<int>(type: "int", nullable: false),
                    VPA_MovimientoCajaId = table.Column<int>(type: "int", nullable: true),
                    VPA_Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    VPA_TipoCambio = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VPA_MontoMoneda = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VPA_MontoBase = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VPA_MontoRecibido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VPA_CambioEntregado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VPA_Referencia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    VPA_Autorizacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VPA_UltimosCuatroDigitos = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true),
                    VPA_Anulado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VPA_FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VPA_MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PB_VENTA_PAGO", x => x.VPA_VentaPago);
                    table.CheckConstraint("CK_PB_VENTA_PAGO_MONTOS", "[VPA_TipoCambio] > 0 AND [VPA_MontoMoneda] > 0 AND [VPA_MontoBase] > 0 AND [VPA_MontoRecibido] >= 0 AND [VPA_CambioEntregado] >= 0");
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PAGO_PB_METODO_PAGO_VPA_MetodoPagoId",
                        column: x => x.VPA_MetodoPagoId,
                        principalTable: "PB_METODO_PAGO",
                        principalColumn: "MPA_MetodoPago",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PAGO_PB_MONEDA_VPA_MonedaId",
                        column: x => x.VPA_MonedaId,
                        principalTable: "PB_MONEDA",
                        principalColumn: "MON_Moneda",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PAGO_PB_MOVIMIENTO_CAJA_VPA_MovimientoCajaId",
                        column: x => x.VPA_MovimientoCajaId,
                        principalTable: "PB_MOVIMIENTO_CAJA",
                        principalColumn: "MCA_MovimientoCaja",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PB_VENTA_PAGO_PB_VENTA_VPA_VentaId",
                        column: x => x.VPA_VentaId,
                        principalTable: "PB_VENTA",
                        principalColumn: "VEN_Venta",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GEO_DEPARTAMENTO_DEP_PaisId_DEP_Codigo",
                table: "GEO_DEPARTAMENTO",
                columns: new[] { "DEP_PaisId", "DEP_Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GEO_DEPARTAMENTO_DEP_PaisId_DEP_Nombre",
                table: "GEO_DEPARTAMENTO",
                columns: new[] { "DEP_PaisId", "DEP_Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GEO_MUNICIPIO_MUN_DepartamentoId_MUN_Codigo",
                table: "GEO_MUNICIPIO",
                columns: new[] { "MUN_DepartamentoId", "MUN_Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GEO_MUNICIPIO_MUN_DepartamentoId_MUN_Nombre",
                table: "GEO_MUNICIPIO",
                columns: new[] { "MUN_DepartamentoId", "MUN_Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GEO_PAIS_PAI_Codigo",
                table: "GEO_PAIS",
                column: "PAI_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GEO_PAIS_PAI_Nombre",
                table: "GEO_PAIS",
                column: "PAI_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_AJUSTE_INVENTARIO_AJU_AlmacenId",
                table: "PB_AJUSTE_INVENTARIO",
                column: "AJU_AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_AJUSTE_INVENTARIO_AJU_EmpleadoId",
                table: "PB_AJUSTE_INVENTARIO",
                column: "AJU_EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_AJUSTE_INVENTARIO_AJU_MotivoAjusteId",
                table: "PB_AJUSTE_INVENTARIO",
                column: "AJU_MotivoAjusteId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_AJUSTE_INVENTARIO_AJU_MovimientoInventarioId",
                table: "PB_AJUSTE_INVENTARIO",
                column: "AJU_MovimientoInventarioId",
                unique: true,
                filter: "[AJU_MovimientoInventarioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_AJUSTE_INVENTARIO_AJU_Numero",
                table: "PB_AJUSTE_INVENTARIO",
                column: "AJU_Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_AJUSTE_INVENTARIO_DETALLE_AID_AjusteInventarioId_AID_LibroId",
                table: "PB_AJUSTE_INVENTARIO_DETALLE",
                columns: new[] { "AID_AjusteInventarioId", "AID_LibroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_AJUSTE_INVENTARIO_DETALLE_AID_LibroId",
                table: "PB_AJUSTE_INVENTARIO_DETALLE",
                column: "AID_LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_ALMACEN_ALM_SucursalId_ALM_Codigo",
                table: "PB_ALMACEN",
                columns: new[] { "ALM_SucursalId", "ALM_Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ALMACEN_ALM_SucursalId_ALM_EsPrincipal",
                table: "PB_ALMACEN",
                columns: new[] { "ALM_SucursalId", "ALM_EsPrincipal" },
                unique: true,
                filter: "[ALM_EsPrincipal] = 1 AND [ALM_Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PB_ALMACEN_ALM_SucursalId_ALM_Nombre",
                table: "PB_ALMACEN",
                columns: new[] { "ALM_SucursalId", "ALM_Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_APERTURA_CAJA_ACA_CajaId_ACA_Abierta",
                table: "PB_APERTURA_CAJA",
                columns: new[] { "ACA_CajaId", "ACA_Abierta" },
                unique: true,
                filter: "[ACA_Abierta] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PB_APERTURA_CAJA_ACA_EmpleadoId",
                table: "PB_APERTURA_CAJA",
                column: "ACA_EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_APERTURA_CAJA_ACA_FechaApertura",
                table: "PB_APERTURA_CAJA",
                column: "ACA_FechaApertura");

            migrationBuilder.CreateIndex(
                name: "IX_PB_APERTURA_CAJA_ACA_MonedaId",
                table: "PB_APERTURA_CAJA",
                column: "ACA_MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_AUDITORIA_AUD_Accion_AUD_Fecha",
                table: "PB_AUDITORIA",
                columns: new[] { "AUD_Accion", "AUD_Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_PB_AUDITORIA_AUD_CorrelacionId",
                table: "PB_AUDITORIA",
                column: "AUD_CorrelacionId",
                filter: "[AUD_CorrelacionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_AUDITORIA_AUD_Entidad_AUD_EntidadId",
                table: "PB_AUDITORIA",
                columns: new[] { "AUD_Entidad", "AUD_EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_PB_AUDITORIA_AUD_Fecha",
                table: "PB_AUDITORIA",
                column: "AUD_Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_PB_AUDITORIA_AUD_UsuarioId",
                table: "PB_AUDITORIA",
                column: "AUD_UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_AUDITORIA_DETALLE_ADE_AuditoriaId",
                table: "PB_AUDITORIA_DETALLE",
                column: "ADE_AuditoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_AUTOR_AUT_PrimerNombre_AUT_PrimerApellido",
                table: "PB_AUTOR",
                columns: new[] { "AUT_PrimerNombre", "AUT_PrimerApellido" });

            migrationBuilder.CreateIndex(
                name: "IX_PB_CAJA_CAJ_SucursalId_CAJ_Codigo",
                table: "PB_CAJA",
                columns: new[] { "CAJ_SucursalId", "CAJ_Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_CAJA_CAJ_SucursalId_CAJ_Nombre",
                table: "PB_CAJA",
                columns: new[] { "CAJ_SucursalId", "CAJ_Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_CATEGORIA_CAT_CategoriaPadreId_CAT_Nombre",
                table: "PB_CATEGORIA",
                columns: new[] { "CAT_CategoriaPadreId", "CAT_Nombre" },
                unique: true,
                filter: "[CAT_CategoriaPadreId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_CATEGORIA_CAT_Codigo",
                table: "PB_CATEGORIA",
                column: "CAT_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_CIERRE_CAJA_CCA_AperturaCajaId",
                table: "PB_CIERRE_CAJA",
                column: "CCA_AperturaCajaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_CIERRE_CAJA_CCA_EmpleadoId",
                table: "PB_CIERRE_CAJA",
                column: "CCA_EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_CIERRE_CAJA_CCA_FechaCierre",
                table: "PB_CIERRE_CAJA",
                column: "CCA_FechaCierre");

            migrationBuilder.CreateIndex(
                name: "IX_PB_CIERRE_CAJA_DETALLE_CCD_CierreCajaId_CCD_MetodoPagoId",
                table: "PB_CIERRE_CAJA_DETALLE",
                columns: new[] { "CCD_CierreCajaId", "CCD_MetodoPagoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_CIERRE_CAJA_DETALLE_CCD_MetodoPagoId",
                table: "PB_CIERRE_CAJA_DETALLE",
                column: "CCD_MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_CLIENTE_CLI_Codigo",
                table: "PB_CLIENTE",
                column: "CLI_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_CLIENTE_CLI_PersonaId",
                table: "PB_CLIENTE",
                column: "CLI_PersonaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_PAGO_DPA_DevolucionVentaId",
                table: "PB_DEVOLUCION_PAGO",
                column: "DPA_DevolucionVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_PAGO_DPA_MetodoPagoId",
                table: "PB_DEVOLUCION_PAGO",
                column: "DPA_MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_PAGO_DPA_MonedaId",
                table: "PB_DEVOLUCION_PAGO",
                column: "DPA_MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_PAGO_DPA_MovimientoCajaId",
                table: "PB_DEVOLUCION_PAGO",
                column: "DPA_MovimientoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DEV_AlmacenId",
                table: "PB_DEVOLUCION_VENTA",
                column: "DEV_AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DEV_AperturaCajaId",
                table: "PB_DEVOLUCION_VENTA",
                column: "DEV_AperturaCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DEV_EmpleadoId",
                table: "PB_DEVOLUCION_VENTA",
                column: "DEV_EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DEV_EstadoDevolucionId",
                table: "PB_DEVOLUCION_VENTA",
                column: "DEV_EstadoDevolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DEV_Fecha",
                table: "PB_DEVOLUCION_VENTA",
                column: "DEV_Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DEV_MonedaId",
                table: "PB_DEVOLUCION_VENTA",
                column: "DEV_MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DEV_Numero",
                table: "PB_DEVOLUCION_VENTA",
                column: "DEV_Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DEV_VentaId",
                table: "PB_DEVOLUCION_VENTA",
                column: "DEV_VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DETALLE_DVD_DevolucionVentaId_DVD_VentaDetalleId",
                table: "PB_DEVOLUCION_VENTA_DETALLE",
                columns: new[] { "DVD_DevolucionVentaId", "DVD_VentaDetalleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DETALLE_DVD_LibroId",
                table: "PB_DEVOLUCION_VENTA_DETALLE",
                column: "DVD_LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DETALLE_DVD_MotivoDevolucionId",
                table: "PB_DEVOLUCION_VENTA_DETALLE",
                column: "DVD_MotivoDevolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DEVOLUCION_VENTA_DETALLE_DVD_VentaDetalleId",
                table: "PB_DEVOLUCION_VENTA_DETALLE",
                column: "DVD_VentaDetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_DIRECCION_DIR_MunicipioId",
                table: "PB_DIRECCION",
                column: "DIR_MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_EDITORIAL_EDI_Codigo",
                table: "PB_EDITORIAL",
                column: "EDI_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_EDITORIAL_EDI_Nombre",
                table: "PB_EDITORIAL",
                column: "EDI_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_EMPLEADO_EMP_Codigo",
                table: "PB_EMPLEADO",
                column: "EMP_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_EMPLEADO_EMP_PersonaId",
                table: "PB_EMPLEADO",
                column: "EMP_PersonaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_EMPLEADO_EMP_PuestoId",
                table: "PB_EMPLEADO",
                column: "EMP_PuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_EMPLEADO_EMP_SucursalId",
                table: "PB_EMPLEADO",
                column: "EMP_SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_EMPRESA_EMP_Codigo",
                table: "PB_EMPRESA",
                column: "EMP_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_EMPRESA_EMP_MonedaPredeterminadaId",
                table: "PB_EMPRESA",
                column: "EMP_MonedaPredeterminadaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_EMPRESA_EMP_NombreComercial",
                table: "PB_EMPRESA",
                column: "EMP_NombreComercial");

            migrationBuilder.CreateIndex(
                name: "IX_PB_EMPRESA_EMP_PersonaId",
                table: "PB_EMPRESA",
                column: "EMP_PersonaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ESTADO_DEVOLUCION_EDV_Codigo",
                table: "PB_ESTADO_DEVOLUCION",
                column: "EDV_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ESTADO_DEVOLUCION_EDV_Nombre",
                table: "PB_ESTADO_DEVOLUCION",
                column: "EDV_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ESTADO_ORDEN_COMPRA_EOC_Codigo",
                table: "PB_ESTADO_ORDEN_COMPRA",
                column: "EOC_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ESTADO_ORDEN_COMPRA_EOC_Nombre",
                table: "PB_ESTADO_ORDEN_COMPRA",
                column: "EOC_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ESTADO_PEDIDO_EPE_Codigo",
                table: "PB_ESTADO_PEDIDO",
                column: "EPE_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ESTADO_PEDIDO_EPE_Nombre",
                table: "PB_ESTADO_PEDIDO",
                column: "EPE_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ESTADO_RECEPCION_COMPRA_ERC_Codigo",
                table: "PB_ESTADO_RECEPCION_COMPRA",
                column: "ERC_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ESTADO_RECEPCION_COMPRA_ERC_Nombre",
                table: "PB_ESTADO_RECEPCION_COMPRA",
                column: "ERC_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ESTADO_VENTA_EVE_Codigo",
                table: "PB_ESTADO_VENTA",
                column: "EVE_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ESTADO_VENTA_EVE_Nombre",
                table: "PB_ESTADO_VENTA",
                column: "EVE_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_EXISTENCIA_EXI_AlmacenId_EXI_LibroId",
                table: "PB_EXISTENCIA",
                columns: new[] { "EXI_AlmacenId", "EXI_LibroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_EXISTENCIA_EXI_LibroId",
                table: "PB_EXISTENCIA",
                column: "EXI_LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_FORMATO_LIBRO_FLI_Codigo",
                table: "PB_FORMATO_LIBRO",
                column: "FLI_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_FORMATO_LIBRO_FLI_Nombre",
                table: "PB_FORMATO_LIBRO",
                column: "FLI_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_HISTORIAL_ACCESO_HAC_CorrelacionId",
                table: "PB_HISTORIAL_ACCESO",
                column: "HAC_CorrelacionId",
                filter: "[HAC_CorrelacionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_HISTORIAL_ACCESO_HAC_Exitoso_HAC_Fecha",
                table: "PB_HISTORIAL_ACCESO",
                columns: new[] { "HAC_Exitoso", "HAC_Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_PB_HISTORIAL_ACCESO_HAC_Fecha",
                table: "PB_HISTORIAL_ACCESO",
                column: "HAC_Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_PB_HISTORIAL_ACCESO_HAC_NombreUsuario_HAC_Fecha",
                table: "PB_HISTORIAL_ACCESO",
                columns: new[] { "HAC_NombreUsuario", "HAC_Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_PB_HISTORIAL_ACCESO_HAC_UsuarioId",
                table: "PB_HISTORIAL_ACCESO",
                column: "HAC_UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_IDIOMA_IDI_Codigo",
                table: "PB_IDIOMA",
                column: "IDI_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_IDIOMA_IDI_Nombre",
                table: "PB_IDIOMA",
                column: "IDI_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_IMPUESTO_IMP_Codigo",
                table: "PB_IMPUESTO",
                column: "IMP_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_LIB_Codigo",
                table: "PB_LIBRO",
                column: "LIB_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_LIB_CodigoBarras",
                table: "PB_LIBRO",
                column: "LIB_CodigoBarras",
                unique: true,
                filter: "[LIB_CodigoBarras] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_LIB_EditorialId",
                table: "PB_LIBRO",
                column: "LIB_EditorialId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_LIB_FormatoLibroId",
                table: "PB_LIBRO",
                column: "LIB_FormatoLibroId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_LIB_IdiomaId",
                table: "PB_LIBRO",
                column: "LIB_IdiomaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_LIB_ImpuestoId",
                table: "PB_LIBRO",
                column: "LIB_ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_LIB_ISBN10",
                table: "PB_LIBRO",
                column: "LIB_ISBN10",
                unique: true,
                filter: "[LIB_ISBN10] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_LIB_ISBN13",
                table: "PB_LIBRO",
                column: "LIB_ISBN13",
                unique: true,
                filter: "[LIB_ISBN13] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_LIB_Titulo",
                table: "PB_LIBRO",
                column: "LIB_Titulo");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_AUTOR_LAU_AutorId",
                table: "PB_LIBRO_AUTOR",
                column: "LAU_AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_CATEGORIA_LCA_CategoriaId",
                table: "PB_LIBRO_CATEGORIA",
                column: "LCA_CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_CATEGORIA_LCA_LibroId_LCA_Principal",
                table: "PB_LIBRO_CATEGORIA",
                columns: new[] { "LCA_LibroId", "LCA_Principal" },
                unique: true,
                filter: "[LCA_Principal] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_PRECIO_LIP_LibroId_LIP_ListaPrecioId_LIP_Activo",
                table: "PB_LIBRO_PRECIO",
                columns: new[] { "LIP_LibroId", "LIP_ListaPrecioId", "LIP_Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_PRECIO_LIP_LibroId_LIP_ListaPrecioId_LIP_FechaInicio",
                table: "PB_LIBRO_PRECIO",
                columns: new[] { "LIP_LibroId", "LIP_ListaPrecioId", "LIP_FechaInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_LIBRO_PRECIO_LIP_ListaPrecioId",
                table: "PB_LIBRO_PRECIO",
                column: "LIP_ListaPrecioId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LISTA_PRECIO_LPR_Codigo",
                table: "PB_LISTA_PRECIO",
                column: "LPR_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_LISTA_PRECIO_LPR_EsPredeterminada",
                table: "PB_LISTA_PRECIO",
                column: "LPR_EsPredeterminada",
                unique: true,
                filter: "[LPR_EsPredeterminada] = 1 AND [LPR_Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LISTA_PRECIO_LPR_Nombre",
                table: "PB_LISTA_PRECIO",
                column: "LPR_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_LOTE_LOT_AlmacenId_LOT_LibroId_LOT_Numero",
                table: "PB_LOTE",
                columns: new[] { "LOT_AlmacenId", "LOT_LibroId", "LOT_Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_LOTE_LOT_LibroId",
                table: "PB_LOTE",
                column: "LOT_LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_LOTE_LOT_ProveedorId",
                table: "PB_LOTE",
                column: "LOT_ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_METODO_PAGO_MPA_Codigo",
                table: "PB_METODO_PAGO",
                column: "MPA_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_METODO_PAGO_MPA_Nombre",
                table: "PB_METODO_PAGO",
                column: "MPA_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_MONEDA_MON_Codigo",
                table: "PB_MONEDA",
                column: "MON_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_MONEDA_MON_EsPredeterminada",
                table: "PB_MONEDA",
                column: "MON_EsPredeterminada",
                unique: true,
                filter: "[MON_EsPredeterminada] = 1 AND [MON_Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MONEDA_MON_Nombre",
                table: "PB_MONEDA",
                column: "MON_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOTIVO_AJUSTE_MAJ_Codigo",
                table: "PB_MOTIVO_AJUSTE",
                column: "MAJ_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOTIVO_AJUSTE_MAJ_Nombre",
                table: "PB_MOTIVO_AJUSTE",
                column: "MAJ_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOTIVO_DEVOLUCION_MDV_Codigo",
                table: "PB_MOTIVO_DEVOLUCION",
                column: "MDV_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOTIVO_DEVOLUCION_MDV_Nombre",
                table: "PB_MOTIVO_DEVOLUCION",
                column: "MDV_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_CAJA_MCA_AperturaCajaId_MCA_Fecha",
                table: "PB_MOVIMIENTO_CAJA",
                columns: new[] { "MCA_AperturaCajaId", "MCA_Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_CAJA_MCA_DevolucionVentaId",
                table: "PB_MOVIMIENTO_CAJA",
                column: "MCA_DevolucionVentaId",
                filter: "[MCA_DevolucionVentaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_CAJA_MCA_EmpleadoId",
                table: "PB_MOVIMIENTO_CAJA",
                column: "MCA_EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_CAJA_MCA_MetodoPagoId",
                table: "PB_MOVIMIENTO_CAJA",
                column: "MCA_MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_CAJA_MCA_MonedaId",
                table: "PB_MOVIMIENTO_CAJA",
                column: "MCA_MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_CAJA_MCA_Numero",
                table: "PB_MOVIMIENTO_CAJA",
                column: "MCA_Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_CAJA_MCA_TipoMovimientoCajaId",
                table: "PB_MOVIMIENTO_CAJA",
                column: "MCA_TipoMovimientoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_CAJA_MCA_VentaId",
                table: "PB_MOVIMIENTO_CAJA",
                column: "MCA_VentaId",
                filter: "[MCA_VentaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_INVENTARIO_MOV_AlmacenDestinoId",
                table: "PB_MOVIMIENTO_INVENTARIO",
                column: "MOV_AlmacenDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_INVENTARIO_MOV_AlmacenOrigenId",
                table: "PB_MOVIMIENTO_INVENTARIO",
                column: "MOV_AlmacenOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_INVENTARIO_MOV_EmpleadoId",
                table: "PB_MOVIMIENTO_INVENTARIO",
                column: "MOV_EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_INVENTARIO_MOV_Fecha",
                table: "PB_MOVIMIENTO_INVENTARIO",
                column: "MOV_Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_INVENTARIO_MOV_Numero",
                table: "PB_MOVIMIENTO_INVENTARIO",
                column: "MOV_Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_INVENTARIO_MOV_RecepcionCompraId",
                table: "PB_MOVIMIENTO_INVENTARIO",
                column: "MOV_RecepcionCompraId",
                filter: "[MOV_RecepcionCompraId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_INVENTARIO_MOV_TipoMovimientoId",
                table: "PB_MOVIMIENTO_INVENTARIO",
                column: "MOV_TipoMovimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_INVENTARIO_DETALLE_MDE_LibroId",
                table: "PB_MOVIMIENTO_INVENTARIO_DETALLE",
                column: "MDE_LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_INVENTARIO_DETALLE_MDE_LoteId",
                table: "PB_MOVIMIENTO_INVENTARIO_DETALLE",
                column: "MDE_LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_MOVIMIENTO_INVENTARIO_DETALLE_MDE_MovimientoInventarioId_MDE_LibroId_MDE_LoteId",
                table: "PB_MOVIMIENTO_INVENTARIO_DETALLE",
                columns: new[] { "MDE_MovimientoInventarioId", "MDE_LibroId", "MDE_LoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_PB_ORDEN_COMPRA_OCO_EmpleadoSolicitanteId",
                table: "PB_ORDEN_COMPRA",
                column: "OCO_EmpleadoSolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_ORDEN_COMPRA_OCO_EstadoOrdenCompraId",
                table: "PB_ORDEN_COMPRA",
                column: "OCO_EstadoOrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_ORDEN_COMPRA_OCO_FechaEmision",
                table: "PB_ORDEN_COMPRA",
                column: "OCO_FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_PB_ORDEN_COMPRA_OCO_MonedaId",
                table: "PB_ORDEN_COMPRA",
                column: "OCO_MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_ORDEN_COMPRA_OCO_Numero",
                table: "PB_ORDEN_COMPRA",
                column: "OCO_Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_ORDEN_COMPRA_OCO_ProveedorId",
                table: "PB_ORDEN_COMPRA",
                column: "OCO_ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_ORDEN_COMPRA_OCO_SucursalId",
                table: "PB_ORDEN_COMPRA",
                column: "OCO_SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_ORDEN_COMPRA_DETALLE_OCD_LibroId",
                table: "PB_ORDEN_COMPRA_DETALLE",
                column: "OCD_LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_ORDEN_COMPRA_DETALLE_OCD_OrdenCompraId_OCD_LibroId",
                table: "PB_ORDEN_COMPRA_DETALLE",
                columns: new[] { "OCD_OrdenCompraId", "OCD_LibroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PARAMETRO_SISTEMA_PAR_EmpresaId_PAR_Clave",
                table: "PB_PARAMETRO_SISTEMA",
                columns: new[] { "PAR_EmpresaId", "PAR_Clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_AlmacenId",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_ClienteId",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_EmpleadoId",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_EstadoPedidoId",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_EstadoPedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_Fecha",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_MonedaId",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_Numero",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_PED_SucursalId",
                table: "PB_PEDIDO_CLIENTE",
                column: "PED_SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_DETALLE_PDD_LibroId",
                table: "PB_PEDIDO_CLIENTE_DETALLE",
                column: "PDD_LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_DETALLE_PDD_PedidoClienteId_PDD_LibroId",
                table: "PB_PEDIDO_CLIENTE_DETALLE",
                columns: new[] { "PDD_PedidoClienteId", "PDD_LibroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PEDIDO_CLIENTE_DETALLE_PDD_PedidoClienteId_PDD_NumeroLinea",
                table: "PB_PEDIDO_CLIENTE_DETALLE",
                columns: new[] { "PDD_PedidoClienteId", "PDD_NumeroLinea" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_PER_TipoPersonaId",
                table: "PB_PERSONA",
                column: "PER_TipoPersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_CORREO_PCO_PersonaId_PCO_Correo",
                table: "PB_PERSONA_CORREO",
                columns: new[] { "PCO_PersonaId", "PCO_Correo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_CORREO_PCO_PersonaId_PCO_Principal",
                table: "PB_PERSONA_CORREO",
                columns: new[] { "PCO_PersonaId", "PCO_Principal" },
                unique: true,
                filter: "[PCO_Principal] = 1 AND [PCO_Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_DIRECCION_PDI_DireccionId",
                table: "PB_PERSONA_DIRECCION",
                column: "PDI_DireccionId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_DIRECCION_PDI_PersonaId_PDI_DireccionId_PDI_TipoDireccionId",
                table: "PB_PERSONA_DIRECCION",
                columns: new[] { "PDI_PersonaId", "PDI_DireccionId", "PDI_TipoDireccionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_DIRECCION_PDI_PersonaId_PDI_Principal",
                table: "PB_PERSONA_DIRECCION",
                columns: new[] { "PDI_PersonaId", "PDI_Principal" },
                unique: true,
                filter: "[PDI_Principal] = 1 AND [PDI_Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_DIRECCION_PDI_TipoDireccionId",
                table: "PB_PERSONA_DIRECCION",
                column: "PDI_TipoDireccionId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_IDENTIFICACION_PID_PersonaId_PID_Principal",
                table: "PB_PERSONA_IDENTIFICACION",
                columns: new[] { "PID_PersonaId", "PID_Principal" },
                unique: true,
                filter: "[PID_Principal] = 1 AND [PID_Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_IDENTIFICACION_PID_TipoIdentificacionId_PID_Numero",
                table: "PB_PERSONA_IDENTIFICACION",
                columns: new[] { "PID_TipoIdentificacionId", "PID_Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_TELEFONO_PTE_PersonaId_PTE_Numero",
                table: "PB_PERSONA_TELEFONO",
                columns: new[] { "PTE_PersonaId", "PTE_Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_TELEFONO_PTE_PersonaId_PTE_Principal",
                table: "PB_PERSONA_TELEFONO",
                columns: new[] { "PTE_PersonaId", "PTE_Principal" },
                unique: true,
                filter: "[PTE_Principal] = 1 AND [PTE_Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PERSONA_TELEFONO_PTE_TipoTelefonoId",
                table: "PB_PERSONA_TELEFONO",
                column: "PTE_TipoTelefonoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_PROVEEDOR_PRO_Codigo",
                table: "PB_PROVEEDOR",
                column: "PRO_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PROVEEDOR_PRO_PersonaId",
                table: "PB_PROVEEDOR",
                column: "PRO_PersonaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_PUESTO_PUE_Nombre",
                table: "PB_PUESTO",
                column: "PUE_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_REC_AlmacenId",
                table: "PB_RECEPCION_COMPRA",
                column: "REC_AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_REC_EmpleadoId",
                table: "PB_RECEPCION_COMPRA",
                column: "REC_EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_REC_EstadoRecepcionCompraId",
                table: "PB_RECEPCION_COMPRA",
                column: "REC_EstadoRecepcionCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_REC_FechaRecepcion",
                table: "PB_RECEPCION_COMPRA",
                column: "REC_FechaRecepcion");

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_REC_Numero",
                table: "PB_RECEPCION_COMPRA",
                column: "REC_Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_REC_OrdenCompraId",
                table: "PB_RECEPCION_COMPRA",
                column: "REC_OrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_REC_SerieDocumentoProveedor_REC_NumeroDocumentoProveedor",
                table: "PB_RECEPCION_COMPRA",
                columns: new[] { "REC_SerieDocumentoProveedor", "REC_NumeroDocumentoProveedor" },
                filter: "[REC_NumeroDocumentoProveedor] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_DETALLE_RCD_LibroId",
                table: "PB_RECEPCION_COMPRA_DETALLE",
                column: "RCD_LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_DETALLE_RCD_LoteId",
                table: "PB_RECEPCION_COMPRA_DETALLE",
                column: "RCD_LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_DETALLE_RCD_OrdenCompraDetalleId",
                table: "PB_RECEPCION_COMPRA_DETALLE",
                column: "RCD_OrdenCompraDetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_RECEPCION_COMPRA_DETALLE_RCD_RecepcionCompraId_RCD_OrdenCompraDetalleId",
                table: "PB_RECEPCION_COMPRA_DETALLE",
                columns: new[] { "RCD_RecepcionCompraId", "RCD_OrdenCompraDetalleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_SECUENCIA_DOCUMENTO_SEC_EmpresaId_SEC_SucursalId_SEC_TipoDocumento_SEC_Serie",
                table: "PB_SECUENCIA_DOCUMENTO",
                columns: new[] { "SEC_EmpresaId", "SEC_SucursalId", "SEC_TipoDocumento", "SEC_Serie" },
                unique: true,
                filter: "[SEC_SucursalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_SECUENCIA_DOCUMENTO_SEC_SucursalId",
                table: "PB_SECUENCIA_DOCUMENTO",
                column: "SEC_SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_SUCURSAL_SUC_DireccionId",
                table: "PB_SUCURSAL",
                column: "SUC_DireccionId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_SUCURSAL_SUC_EmpresaId_SUC_Codigo",
                table: "PB_SUCURSAL",
                columns: new[] { "SUC_EmpresaId", "SUC_Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_SUCURSAL_SUC_EmpresaId_SUC_EsPrincipal",
                table: "PB_SUCURSAL",
                columns: new[] { "SUC_EmpresaId", "SUC_EsPrincipal" },
                unique: true,
                filter: "[SUC_EsPrincipal] = 1 AND [SUC_Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PB_SUCURSAL_SUC_EmpresaId_SUC_Nombre",
                table: "PB_SUCURSAL",
                columns: new[] { "SUC_EmpresaId", "SUC_Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_DIRECCION_TDI_Codigo",
                table: "PB_TIPO_DIRECCION",
                column: "TDI_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_DIRECCION_TDI_Nombre",
                table: "PB_TIPO_DIRECCION",
                column: "TDI_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_IDENTIFICACION_TID_Codigo",
                table: "PB_TIPO_IDENTIFICACION",
                column: "TID_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_IDENTIFICACION_TID_Nombre",
                table: "PB_TIPO_IDENTIFICACION",
                column: "TID_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_MOVIMIENTO_CAJA_TMC_Codigo",
                table: "PB_TIPO_MOVIMIENTO_CAJA",
                column: "TMC_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_MOVIMIENTO_CAJA_TMC_Nombre",
                table: "PB_TIPO_MOVIMIENTO_CAJA",
                column: "TMC_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_MOVIMIENTO_INVENTARIO_TMI_Codigo",
                table: "PB_TIPO_MOVIMIENTO_INVENTARIO",
                column: "TMI_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_MOVIMIENTO_INVENTARIO_TMI_Nombre",
                table: "PB_TIPO_MOVIMIENTO_INVENTARIO",
                column: "TMI_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_PERSONA_TPR_Codigo",
                table: "PB_TIPO_PERSONA",
                column: "TPR_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_PERSONA_TPR_Nombre",
                table: "PB_TIPO_PERSONA",
                column: "TPR_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_TELEFONO_TTE_Codigo",
                table: "PB_TIPO_TELEFONO",
                column: "TTE_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_TELEFONO_TTE_Nombre",
                table: "PB_TIPO_TELEFONO",
                column: "TTE_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_VENTA_TVE_Codigo",
                table: "PB_TIPO_VENTA",
                column: "TVE_Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_TIPO_VENTA_TVE_Nombre",
                table: "PB_TIPO_VENTA",
                column: "TVE_Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_AlmacenId",
                table: "PB_VENTA",
                column: "VEN_AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_AperturaCajaId",
                table: "PB_VENTA",
                column: "VEN_AperturaCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_ClienteId",
                table: "PB_VENTA",
                column: "VEN_ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_EmpleadoId",
                table: "PB_VENTA",
                column: "VEN_EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_EstadoVentaId",
                table: "PB_VENTA",
                column: "VEN_EstadoVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_Fecha",
                table: "PB_VENTA",
                column: "VEN_Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_MonedaId",
                table: "PB_VENTA",
                column: "VEN_MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_Numero",
                table: "PB_VENTA",
                column: "VEN_Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_PedidoClienteId",
                table: "PB_VENTA",
                column: "VEN_PedidoClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_SucursalId",
                table: "PB_VENTA",
                column: "VEN_SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_VEN_TipoVentaId",
                table: "PB_VENTA",
                column: "VEN_TipoVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_DETALLE_VDE_LibroId",
                table: "PB_VENTA_DETALLE",
                column: "VDE_LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_DETALLE_VDE_VentaId_VDE_LibroId",
                table: "PB_VENTA_DETALLE",
                columns: new[] { "VDE_VentaId", "VDE_LibroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_DETALLE_VDE_VentaId_VDE_NumeroLinea",
                table: "PB_VENTA_DETALLE",
                columns: new[] { "VDE_VentaId", "VDE_NumeroLinea" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_PAGO_VPA_MetodoPagoId",
                table: "PB_VENTA_PAGO",
                column: "VPA_MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_PAGO_VPA_MonedaId",
                table: "PB_VENTA_PAGO",
                column: "VPA_MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_PAGO_VPA_MovimientoCajaId",
                table: "PB_VENTA_PAGO",
                column: "VPA_MovimientoCajaId",
                filter: "[VPA_MovimientoCajaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PB_VENTA_PAGO_VPA_VentaId",
                table: "PB_VENTA_PAGO",
                column: "VPA_VentaId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "SEG_ROL",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SEG_ROL_CLAIM_RoleId_ClaimType_ClaimValue",
                table: "SEG_ROL_CLAIM",
                columns: new[] { "RoleId", "ClaimType", "ClaimValue" },
                unique: true,
                filter: "[ClaimType] IS NOT NULL AND [ClaimValue] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "SEG_USUARIO",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_SEG_USUARIO_USU_Activo_UserName",
                table: "SEG_USUARIO",
                columns: new[] { "USU_Activo", "UserName" });

            migrationBuilder.CreateIndex(
                name: "IX_SEG_USUARIO_USU_CreadoPorUsuarioId",
                table: "SEG_USUARIO",
                column: "USU_CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SEG_USUARIO_USU_EmpleadoId",
                table: "SEG_USUARIO",
                column: "USU_EmpleadoId",
                unique: true,
                filter: "[USU_EmpleadoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SEG_USUARIO_USU_ModificadoPorUsuarioId",
                table: "SEG_USUARIO",
                column: "USU_ModificadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "SEG_USUARIO",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SEG_USUARIO_CLAIM_UserId",
                table: "SEG_USUARIO_CLAIM",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SEG_USUARIO_LOGIN_UserId",
                table: "SEG_USUARIO_LOGIN",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SEG_USUARIO_ROL_RoleId",
                table: "SEG_USUARIO_ROL",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PB_AJUSTE_INVENTARIO_DETALLE");

            migrationBuilder.DropTable(
                name: "PB_AUDITORIA_DETALLE");

            migrationBuilder.DropTable(
                name: "PB_CIERRE_CAJA_DETALLE");

            migrationBuilder.DropTable(
                name: "PB_DEVOLUCION_PAGO");

            migrationBuilder.DropTable(
                name: "PB_DEVOLUCION_VENTA_DETALLE");

            migrationBuilder.DropTable(
                name: "PB_EXISTENCIA");

            migrationBuilder.DropTable(
                name: "PB_HISTORIAL_ACCESO");

            migrationBuilder.DropTable(
                name: "PB_LIBRO_AUTOR");

            migrationBuilder.DropTable(
                name: "PB_LIBRO_CATEGORIA");

            migrationBuilder.DropTable(
                name: "PB_LIBRO_PRECIO");

            migrationBuilder.DropTable(
                name: "PB_MOVIMIENTO_INVENTARIO_DETALLE");

            migrationBuilder.DropTable(
                name: "PB_PARAMETRO_SISTEMA");

            migrationBuilder.DropTable(
                name: "PB_PEDIDO_CLIENTE_DETALLE");

            migrationBuilder.DropTable(
                name: "PB_PERSONA_CORREO");

            migrationBuilder.DropTable(
                name: "PB_PERSONA_DIRECCION");

            migrationBuilder.DropTable(
                name: "PB_PERSONA_IDENTIFICACION");

            migrationBuilder.DropTable(
                name: "PB_PERSONA_JURIDICA");

            migrationBuilder.DropTable(
                name: "PB_PERSONA_NATURAL");

            migrationBuilder.DropTable(
                name: "PB_PERSONA_TELEFONO");

            migrationBuilder.DropTable(
                name: "PB_RECEPCION_COMPRA_DETALLE");

            migrationBuilder.DropTable(
                name: "PB_SECUENCIA_DOCUMENTO");

            migrationBuilder.DropTable(
                name: "PB_VENTA_PAGO");

            migrationBuilder.DropTable(
                name: "SEG_ROL_CLAIM");

            migrationBuilder.DropTable(
                name: "SEG_USUARIO_CLAIM");

            migrationBuilder.DropTable(
                name: "SEG_USUARIO_LOGIN");

            migrationBuilder.DropTable(
                name: "SEG_USUARIO_ROL");

            migrationBuilder.DropTable(
                name: "SEG_USUARIO_TOKEN");

            migrationBuilder.DropTable(
                name: "PB_AJUSTE_INVENTARIO");

            migrationBuilder.DropTable(
                name: "PB_AUDITORIA");

            migrationBuilder.DropTable(
                name: "PB_CIERRE_CAJA");

            migrationBuilder.DropTable(
                name: "PB_MOTIVO_DEVOLUCION");

            migrationBuilder.DropTable(
                name: "PB_VENTA_DETALLE");

            migrationBuilder.DropTable(
                name: "PB_AUTOR");

            migrationBuilder.DropTable(
                name: "PB_CATEGORIA");

            migrationBuilder.DropTable(
                name: "PB_LISTA_PRECIO");

            migrationBuilder.DropTable(
                name: "PB_TIPO_DIRECCION");

            migrationBuilder.DropTable(
                name: "PB_TIPO_IDENTIFICACION");

            migrationBuilder.DropTable(
                name: "PB_TIPO_TELEFONO");

            migrationBuilder.DropTable(
                name: "PB_LOTE");

            migrationBuilder.DropTable(
                name: "PB_ORDEN_COMPRA_DETALLE");

            migrationBuilder.DropTable(
                name: "PB_MOVIMIENTO_CAJA");

            migrationBuilder.DropTable(
                name: "SEG_ROL");

            migrationBuilder.DropTable(
                name: "PB_MOTIVO_AJUSTE");

            migrationBuilder.DropTable(
                name: "PB_MOVIMIENTO_INVENTARIO");

            migrationBuilder.DropTable(
                name: "SEG_USUARIO");

            migrationBuilder.DropTable(
                name: "PB_LIBRO");

            migrationBuilder.DropTable(
                name: "PB_DEVOLUCION_VENTA");

            migrationBuilder.DropTable(
                name: "PB_METODO_PAGO");

            migrationBuilder.DropTable(
                name: "PB_TIPO_MOVIMIENTO_CAJA");

            migrationBuilder.DropTable(
                name: "PB_RECEPCION_COMPRA");

            migrationBuilder.DropTable(
                name: "PB_TIPO_MOVIMIENTO_INVENTARIO");

            migrationBuilder.DropTable(
                name: "PB_EDITORIAL");

            migrationBuilder.DropTable(
                name: "PB_FORMATO_LIBRO");

            migrationBuilder.DropTable(
                name: "PB_IDIOMA");

            migrationBuilder.DropTable(
                name: "PB_IMPUESTO");

            migrationBuilder.DropTable(
                name: "PB_ESTADO_DEVOLUCION");

            migrationBuilder.DropTable(
                name: "PB_VENTA");

            migrationBuilder.DropTable(
                name: "PB_ESTADO_RECEPCION_COMPRA");

            migrationBuilder.DropTable(
                name: "PB_ORDEN_COMPRA");

            migrationBuilder.DropTable(
                name: "PB_APERTURA_CAJA");

            migrationBuilder.DropTable(
                name: "PB_ESTADO_VENTA");

            migrationBuilder.DropTable(
                name: "PB_PEDIDO_CLIENTE");

            migrationBuilder.DropTable(
                name: "PB_TIPO_VENTA");

            migrationBuilder.DropTable(
                name: "PB_ESTADO_ORDEN_COMPRA");

            migrationBuilder.DropTable(
                name: "PB_PROVEEDOR");

            migrationBuilder.DropTable(
                name: "PB_CAJA");

            migrationBuilder.DropTable(
                name: "PB_ALMACEN");

            migrationBuilder.DropTable(
                name: "PB_CLIENTE");

            migrationBuilder.DropTable(
                name: "PB_EMPLEADO");

            migrationBuilder.DropTable(
                name: "PB_ESTADO_PEDIDO");

            migrationBuilder.DropTable(
                name: "PB_PUESTO");

            migrationBuilder.DropTable(
                name: "PB_SUCURSAL");

            migrationBuilder.DropTable(
                name: "PB_DIRECCION");

            migrationBuilder.DropTable(
                name: "PB_EMPRESA");

            migrationBuilder.DropTable(
                name: "GEO_MUNICIPIO");

            migrationBuilder.DropTable(
                name: "PB_MONEDA");

            migrationBuilder.DropTable(
                name: "PB_PERSONA");

            migrationBuilder.DropTable(
                name: "GEO_DEPARTAMENTO");

            migrationBuilder.DropTable(
                name: "PB_TIPO_PERSONA");

            migrationBuilder.DropTable(
                name: "GEO_PAIS");
        }
    }
}
