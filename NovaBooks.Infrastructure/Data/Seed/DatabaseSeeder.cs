using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Cash;
using NovaBooks.Domain.Entities.Catalog;
using NovaBooks.Domain.Entities.Geography;
using NovaBooks.Domain.Entities.Inventory;
using NovaBooks.Domain.Entities.Payments;
using NovaBooks.Domain.Entities.People;
using NovaBooks.Domain.Entities.Purchasing;
using NovaBooks.Domain.Entities.Sales;

namespace NovaBooks.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        await SeedGeographyAsync(context, cancellationToken);
        await SeedPeopleCatalogsAsync(context, cancellationToken);
        await SeedPaymentCatalogsAsync(context, cancellationToken);
        await SeedBookCatalogsAsync(context, cancellationToken);
        await SeedPurchasingCatalogsAsync(context, cancellationToken);
        await SeedInventoryCatalogsAsync(context, cancellationToken);
        await SeedSalesCatalogsAsync(context, cancellationToken);
        await SeedCashCatalogsAsync(context, cancellationToken);
    }

    private static async Task SeedGeographyAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        GEO_PAIS? guatemala = await context.Paises
            .FirstOrDefaultAsync(
                x => x.PAI_Codigo == "GT",
                cancellationToken);

        if (guatemala is null)
        {
            guatemala = new GEO_PAIS
            {
                PAI_Codigo = "GT",
                PAI_Nombre = "Guatemala",
                PAI_Activo = true
            };

            context.Paises.Add(guatemala);

            await context.SaveChangesAsync(cancellationToken);
        }

        GEO_DEPARTAMENTO? departamentoGuatemala =
            await context.Departamentos.FirstOrDefaultAsync(
                x => x.DEP_PaisId == guatemala.PAI_Pais &&
                     x.DEP_Codigo == "GUA",
                cancellationToken);

        if (departamentoGuatemala is null)
        {
            departamentoGuatemala = new GEO_DEPARTAMENTO
            {
                DEP_PaisId = guatemala.PAI_Pais,
                DEP_Codigo = "GUA",
                DEP_Nombre = "Guatemala",
                DEP_Activo = true
            };

            context.Departamentos.Add(departamentoGuatemala);

            await context.SaveChangesAsync(cancellationToken);
        }

        bool existeMunicipioGuatemala =
            await context.Municipios.AnyAsync(
                x => x.MUN_DepartamentoId ==
                         departamentoGuatemala.DEP_Departamento &&
                     x.MUN_Codigo == "GUA",
                cancellationToken);

        if (!existeMunicipioGuatemala)
        {
            context.Municipios.Add(new GEO_MUNICIPIO
            {
                MUN_DepartamentoId =
                    departamentoGuatemala.DEP_Departamento,

                MUN_Codigo = "GUA",
                MUN_Nombre = "Guatemala",
                MUN_Activo = true
            });

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedPeopleCatalogsAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.TiposPersona.AnyAsync(cancellationToken))
        {
            context.TiposPersona.AddRange(
                new PB_TIPO_PERSONA
                {
                    TPR_Codigo = "NATURAL",
                    TPR_Nombre = "Persona natural",
                    TPR_Descripcion =
                        "Persona individual o física.",
                    TPR_Activo = true
                },
                new PB_TIPO_PERSONA
                {
                    TPR_Codigo = "JURIDICA",
                    TPR_Nombre = "Persona jurídica",
                    TPR_Descripcion =
                        "Empresa, sociedad u organización.",
                    TPR_Activo = true
                });
        }

        if (!await context.TiposIdentificacion.AnyAsync(
                cancellationToken))
        {
            context.TiposIdentificacion.AddRange(
                new PB_TIPO_IDENTIFICACION
                {
                    TID_Codigo = "DPI",
                    TID_Nombre =
                        "Documento Personal de Identificación",
                    TID_LongitudMinima = 13,
                    TID_LongitudMaxima = 13,
                    TID_Activo = true
                },
                new PB_TIPO_IDENTIFICACION
                {
                    TID_Codigo = "NIT",
                    TID_Nombre =
                        "Número de Identificación Tributaria",
                    TID_LongitudMinima = 5,
                    TID_LongitudMaxima = 20,
                    TID_Activo = true
                },
                new PB_TIPO_IDENTIFICACION
                {
                    TID_Codigo = "PASAPORTE",
                    TID_Nombre = "Pasaporte",
                    TID_LongitudMinima = 5,
                    TID_LongitudMaxima = 30,
                    TID_Activo = true
                });
        }

        if (!await context.TiposTelefono.AnyAsync(
                cancellationToken))
        {
            context.TiposTelefono.AddRange(
                new PB_TIPO_TELEFONO
                {
                    TTE_Codigo = "CELULAR",
                    TTE_Nombre = "Celular",
                    TTE_Activo = true
                },
                new PB_TIPO_TELEFONO
                {
                    TTE_Codigo = "FIJO",
                    TTE_Nombre = "Teléfono fijo",
                    TTE_Activo = true
                },
                new PB_TIPO_TELEFONO
                {
                    TTE_Codigo = "TRABAJO",
                    TTE_Nombre = "Trabajo",
                    TTE_Activo = true
                },
                new PB_TIPO_TELEFONO
                {
                    TTE_Codigo = "WHATSAPP",
                    TTE_Nombre = "WhatsApp",
                    TTE_Activo = true
                });
        }

        if (!await context.TiposDireccion.AnyAsync(
                cancellationToken))
        {
            context.TiposDireccion.AddRange(
                new PB_TIPO_DIRECCION
                {
                    TDI_Codigo = "RESIDENCIA",
                    TDI_Nombre = "Residencia",
                    TDI_Activo = true
                },
                new PB_TIPO_DIRECCION
                {
                    TDI_Codigo = "FISCAL",
                    TDI_Nombre = "Dirección fiscal",
                    TDI_Activo = true
                },
                new PB_TIPO_DIRECCION
                {
                    TDI_Codigo = "COMERCIAL",
                    TDI_Nombre = "Dirección comercial",
                    TDI_Activo = true
                },
                new PB_TIPO_DIRECCION
                {
                    TDI_Codigo = "ENVIO",
                    TDI_Nombre = "Dirección de envío",
                    TDI_Activo = true
                });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPaymentCatalogsAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.Monedas.AnyAsync(cancellationToken))
        {
            context.Monedas.AddRange(
                new PB_MONEDA
                {
                    MON_Codigo = "GTQ",
                    MON_Nombre = "Quetzal",
                    MON_Simbolo = "Q",
                    MON_Decimales = 2,
                    MON_EsPredeterminada = true,
                    MON_Activo = true
                },
                new PB_MONEDA
                {
                    MON_Codigo = "USD",
                    MON_Nombre = "Dólar estadounidense",
                    MON_Simbolo = "$",
                    MON_Decimales = 2,
                    MON_EsPredeterminada = false,
                    MON_Activo = true
                });
        }

        if (!await context.MetodosPago.AnyAsync(cancellationToken))
        {
            context.MetodosPago.AddRange(
                new PB_METODO_PAGO
                {
                    MPA_Codigo = "EFECTIVO",
                    MPA_Nombre = "Efectivo",
                    MPA_RequiereReferencia = false,
                    MPA_RequiereAutorizacion = false,
                    MPA_AfectaEfectivo = true,
                    MPA_PermiteCambio = true,
                    MPA_Activo = true
                },
                new PB_METODO_PAGO
                {
                    MPA_Codigo = "TARJETA",
                    MPA_Nombre = "Tarjeta",
                    MPA_RequiereReferencia = true,
                    MPA_RequiereAutorizacion = true,
                    MPA_AfectaEfectivo = false,
                    MPA_PermiteCambio = false,
                    MPA_Activo = true
                },
                new PB_METODO_PAGO
                {
                    MPA_Codigo = "TRANSFERENCIA",
                    MPA_Nombre = "Transferencia bancaria",
                    MPA_RequiereReferencia = true,
                    MPA_RequiereAutorizacion = false,
                    MPA_AfectaEfectivo = false,
                    MPA_PermiteCambio = false,
                    MPA_Activo = true
                },
                new PB_METODO_PAGO
                {
                    MPA_Codigo = "CREDITO",
                    MPA_Nombre = "Crédito",
                    MPA_RequiereReferencia = false,
                    MPA_RequiereAutorizacion = true,
                    MPA_AfectaEfectivo = false,
                    MPA_PermiteCambio = false,
                    MPA_Activo = true
                });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedBookCatalogsAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.Idiomas.AnyAsync(cancellationToken))
        {
            context.Idiomas.AddRange(
                new PB_IDIOMA
                {
                    IDI_Codigo = "ES",
                    IDI_Nombre = "Español",
                    IDI_Activo = true
                },
                new PB_IDIOMA
                {
                    IDI_Codigo = "EN",
                    IDI_Nombre = "Inglés",
                    IDI_Activo = true
                });
        }

        if (!await context.FormatosLibro.AnyAsync(
                cancellationToken))
        {
            context.FormatosLibro.AddRange(
                new PB_FORMATO_LIBRO
                {
                    FLI_Codigo = "TAPA_DURA",
                    FLI_Nombre = "Tapa dura",
                    FLI_Activo = true
                },
                new PB_FORMATO_LIBRO
                {
                    FLI_Codigo = "TAPA_BLANDA",
                    FLI_Nombre = "Tapa blanda",
                    FLI_Activo = true
                },
                new PB_FORMATO_LIBRO
                {
                    FLI_Codigo = "BOLSILLO",
                    FLI_Nombre = "Edición de bolsillo",
                    FLI_Activo = true
                },
                new PB_FORMATO_LIBRO
                {
                    FLI_Codigo = "ESPIRAL",
                    FLI_Nombre = "Espiral",
                    FLI_Activo = true
                });
        }

        if (!await context.Impuestos.AnyAsync(cancellationToken))
        {
            context.Impuestos.Add(new PB_IMPUESTO
            {
                IMP_Codigo = "IVA12",
                IMP_Nombre = "IVA 12%",
                IMP_Porcentaje = 12m,
                IMP_FechaInicio = new DateOnly(2000, 1, 1),
                IMP_Activo = true
            });
        }

        if (!await context.ListasPrecio.AnyAsync(
                cancellationToken))
        {
            context.ListasPrecio.AddRange(
                new PB_LISTA_PRECIO
                {
                    LPR_Codigo = "GENERAL",
                    LPR_Nombre = "Precio general",
                    LPR_EsPredeterminada = true,
                    LPR_Activo = true
                },
                new PB_LISTA_PRECIO
                {
                    LPR_Codigo = "MAYORISTA",
                    LPR_Nombre = "Precio mayorista",
                    LPR_EsPredeterminada = false,
                    LPR_Activo = true
                });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPurchasingCatalogsAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.EstadosOrdenCompra.AnyAsync(
                cancellationToken))
        {
            context.EstadosOrdenCompra.AddRange(
                CreateEstadoOrden("BORRADOR", "Borrador"),
                CreateEstadoOrden("EMITIDA", "Emitida"),
                CreateEstadoOrden(
                    "PARCIAL",
                    "Recibida parcialmente"),
                CreateEstadoOrden(
                    "RECIBIDA",
                    "Recibida completamente"),
                CreateEstadoOrden("CANCELADA", "Cancelada"));
        }

        if (!await context.EstadosRecepcionCompra.AnyAsync(
                cancellationToken))
        {
            context.EstadosRecepcionCompra.AddRange(
                CreateEstadoRecepcion("BORRADOR", "Borrador"),
                CreateEstadoRecepcion("PROCESADA", "Procesada"),
                CreateEstadoRecepcion("ANULADA", "Anulada"));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedInventoryCatalogsAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.TiposMovimientoInventario.AnyAsync(
                cancellationToken))
        {
            context.TiposMovimientoInventario.AddRange(
                CreateTipoMovimientoInventario(
                    "ENTRADA_COMPRA",
                    "Entrada por compra",
                    1,
                    false,
                    true),

                CreateTipoMovimientoInventario(
                    "SALIDA_VENTA",
                    "Salida por venta",
                    -1,
                    true,
                    false),

                CreateTipoMovimientoInventario(
                    "DEVOLUCION_CLIENTE",
                    "Entrada por devolución del cliente",
                    1,
                    false,
                    true),

                CreateTipoMovimientoInventario(
                    "DEVOLUCION_PROVEEDOR",
                    "Salida por devolución al proveedor",
                    -1,
                    true,
                    false),

                CreateTipoMovimientoInventario(
                    "AJUSTE_POSITIVO",
                    "Ajuste positivo",
                    1,
                    false,
                    true),

                CreateTipoMovimientoInventario(
                    "AJUSTE_NEGATIVO",
                    "Ajuste negativo",
                    -1,
                    true,
                    false),

                CreateTipoMovimientoInventario(
                    "TRANSFERENCIA",
                    "Transferencia entre almacenes",
                    0,
                    true,
                    true));
        }

        if (!await context.MotivosAjuste.AnyAsync(
                cancellationToken))
        {
            context.MotivosAjuste.AddRange(
                CreateMotivoAjuste(
                    "SOBRANTE",
                    "Sobrante físico",
                    1),

                CreateMotivoAjuste(
                    "FALTANTE",
                    "Faltante físico",
                    -1),

                CreateMotivoAjuste(
                    "DANIO",
                    "Producto dañado",
                    -1),

                CreateMotivoAjuste(
                    "ERROR_REGISTRO_POSITIVO",
                    "Corrección positiva",
                    1),

                CreateMotivoAjuste(
                    "ERROR_REGISTRO_NEGATIVO",
                    "Corrección negativa",
                    -1));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSalesCatalogsAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.TiposVenta.AnyAsync(cancellationToken))
        {
            context.TiposVenta.AddRange(
                new PB_TIPO_VENTA
                {
                    TVE_Codigo = "CONTADO",
                    TVE_Nombre = "Contado",
                    TVE_RequiereCliente = false,
                    TVE_GeneraCuentaPorCobrar = false,
                    TVE_Activo = true
                },
                new PB_TIPO_VENTA
                {
                    TVE_Codigo = "CREDITO",
                    TVE_Nombre = "Crédito",
                    TVE_RequiereCliente = true,
                    TVE_GeneraCuentaPorCobrar = true,
                    TVE_Activo = true
                });
        }

        if (!await context.EstadosVenta.AnyAsync(cancellationToken))
        {
            context.EstadosVenta.AddRange(
                CreateEstadoVenta("BORRADOR", "Borrador"),
                CreateEstadoVenta("PENDIENTE", "Pendiente"),
                CreateEstadoVenta("PAGADA", "Pagada"),
                CreateEstadoVenta("PARCIAL", "Pagada parcialmente"),
                CreateEstadoVenta("CREDITO", "Al crédito"),
                CreateEstadoVenta("ANULADA", "Anulada"),
                CreateEstadoVenta("DEVUELTA", "Devuelta"));
        }

        if (!await context.EstadosPedido.AnyAsync(cancellationToken))
        {
            context.EstadosPedido.AddRange(
                CreateEstadoPedido("BORRADOR", "Borrador"),
                CreateEstadoPedido("CONFIRMADO", "Confirmado"),
                CreateEstadoPedido("RESERVADO", "Reservado"),
                CreateEstadoPedido("FACTURADO", "Facturado"),
                CreateEstadoPedido("CANCELADO", "Cancelado"),
                CreateEstadoPedido("VENCIDO", "Vencido"));
        }

        if (!await context.EstadosDevolucion.AnyAsync(
                cancellationToken))
        {
            context.EstadosDevolucion.AddRange(
                CreateEstadoDevolucion("BORRADOR", "Borrador"),
                CreateEstadoDevolucion("PROCESADA", "Procesada"),
                CreateEstadoDevolucion("ANULADA", "Anulada"));
        }

        if (!await context.MotivosDevolucion.AnyAsync(
                cancellationToken))
        {
            context.MotivosDevolucion.AddRange(
                CreateMotivoDevolucion(
                    "DEFECTUOSO",
                    "Producto defectuoso",
                    false),

                CreateMotivoDevolucion(
                    "INCORRECTO",
                    "Producto incorrecto",
                    true),

                CreateMotivoDevolucion(
                    "ERROR_FACTURACION",
                    "Error de facturación",
                    true),

                CreateMotivoDevolucion(
                    "CAMBIO",
                    "Cambio autorizado",
                    true),

                CreateMotivoDevolucion(
                    "DANIO",
                    "Daño físico",
                    false));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedCashCatalogsAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.TiposMovimientoCaja.AnyAsync(
                cancellationToken))
        {
            context.TiposMovimientoCaja.AddRange(
                CreateTipoMovimientoCaja(
                    "VENTA",
                    "Ingreso por venta",
                    1,
                    false),

                CreateTipoMovimientoCaja(
                    "INGRESO_MANUAL",
                    "Ingreso manual",
                    1,
                    true),

                CreateTipoMovimientoCaja(
                    "REEMBOLSO",
                    "Reembolso",
                    -1,
                    true),

                CreateTipoMovimientoCaja(
                    "RETIRO",
                    "Retiro de efectivo",
                    -1,
                    true),

                CreateTipoMovimientoCaja(
                    "GASTO",
                    "Gasto de caja",
                    -1,
                    true),

                CreateTipoMovimientoCaja(
                    "AJUSTE_POSITIVO",
                    "Ajuste positivo",
                    1,
                    true),

                CreateTipoMovimientoCaja(
                    "AJUSTE_NEGATIVO",
                    "Ajuste negativo",
                    -1,
                    true));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static PB_ESTADO_ORDEN_COMPRA CreateEstadoOrden(
        string codigo,
        string nombre)
    {
        return new PB_ESTADO_ORDEN_COMPRA
        {
            EOC_Codigo = codigo,
            EOC_Nombre = nombre,
            EOC_Activo = true
        };
    }

    private static PB_ESTADO_RECEPCION_COMPRA CreateEstadoRecepcion(
        string codigo,
        string nombre)
    {
        return new PB_ESTADO_RECEPCION_COMPRA
        {
            ERC_Codigo = codigo,
            ERC_Nombre = nombre,
            ERC_Activo = true
        };
    }

    private static PB_TIPO_MOVIMIENTO_INVENTARIO
        CreateTipoMovimientoInventario(
            string codigo,
            string nombre,
            short naturaleza,
            bool requiereOrigen,
            bool requiereDestino)
    {
        return new PB_TIPO_MOVIMIENTO_INVENTARIO
        {
            TMI_Codigo = codigo,
            TMI_Nombre = nombre,
            TMI_Naturaleza = naturaleza,
            TMI_RequiereAlmacenOrigen = requiereOrigen,
            TMI_RequiereAlmacenDestino = requiereDestino,
            TMI_Activo = true
        };
    }

    private static PB_MOTIVO_AJUSTE CreateMotivoAjuste(
        string codigo,
        string nombre,
        short naturaleza)
    {
        return new PB_MOTIVO_AJUSTE
        {
            MAJ_Codigo = codigo,
            MAJ_Nombre = nombre,
            MAJ_Naturaleza = naturaleza,
            MAJ_RequiereObservacion = true,
            MAJ_Activo = true
        };
    }

    private static PB_ESTADO_VENTA CreateEstadoVenta(
        string codigo,
        string nombre)
    {
        return new PB_ESTADO_VENTA
        {
            EVE_Codigo = codigo,
            EVE_Nombre = nombre,
            EVE_Activo = true
        };
    }

    private static PB_ESTADO_PEDIDO CreateEstadoPedido(
        string codigo,
        string nombre)
    {
        return new PB_ESTADO_PEDIDO
        {
            EPE_Codigo = codigo,
            EPE_Nombre = nombre,
            EPE_Activo = true
        };
    }

    private static PB_ESTADO_DEVOLUCION CreateEstadoDevolucion(
        string codigo,
        string nombre)
    {
        return new PB_ESTADO_DEVOLUCION
        {
            EDV_Codigo = codigo,
            EDV_Nombre = nombre,
            EDV_Activo = true
        };
    }

    private static PB_MOTIVO_DEVOLUCION CreateMotivoDevolucion(
        string codigo,
        string nombre,
        bool reintegraInventario)
    {
        return new PB_MOTIVO_DEVOLUCION
        {
            MDV_Codigo = codigo,
            MDV_Nombre = nombre,
            MDV_ReintegraInventario = reintegraInventario,
            MDV_RequiereObservacion = true,
            MDV_Activo = true
        };
    }

    private static PB_TIPO_MOVIMIENTO_CAJA
        CreateTipoMovimientoCaja(
            string codigo,
            string nombre,
            short naturaleza,
            bool requiereAutorizacion)
    {
        return new PB_TIPO_MOVIMIENTO_CAJA
        {
            TMC_Codigo = codigo,
            TMC_Nombre = nombre,
            TMC_Naturaleza = naturaleza,
            TMC_RequiereAutorizacion =
                requiereAutorizacion,
            TMC_Activo = true
        };
    }
}