using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Sales;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class SalesConfiguration
{
    public static void ConfigureSales(this ModelBuilder modelBuilder)
    {
        ConfigureTipoVenta(modelBuilder);
        ConfigureEstadoVenta(modelBuilder);
        ConfigureEstadoPedido(modelBuilder);
        ConfigurePedido(modelBuilder);
        ConfigurePedidoDetalle(modelBuilder);
        ConfigureVenta(modelBuilder);
        ConfigureVentaDetalle(modelBuilder);
        ConfigureVentaPago(modelBuilder);
        ConfigureEstadoDevolucion(modelBuilder);
        ConfigureMotivoDevolucion(modelBuilder);
        ConfigureDevolucion(modelBuilder);
        ConfigureDevolucionDetalle(modelBuilder);
        ConfigureDevolucionPago(modelBuilder);
    }

    private static void ConfigureTipoVenta(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_TIPO_VENTA>(builder =>
        {
            builder.ToTable("PB_TIPO_VENTA");
            builder.HasKey(x => x.TVE_TipoVenta);

            builder.Property(x => x.TVE_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.TVE_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.TVE_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.TVE_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.TVE_Codigo).IsUnique();
            builder.HasIndex(x => x.TVE_Nombre).IsUnique();
        });
    }

    private static void ConfigureEstadoVenta(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_ESTADO_VENTA>(builder =>
        {
            builder.ToTable("PB_ESTADO_VENTA");
            builder.HasKey(x => x.EVE_EstadoVenta);

            builder.Property(x => x.EVE_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.EVE_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.EVE_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.EVE_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.EVE_Codigo).IsUnique();
            builder.HasIndex(x => x.EVE_Nombre).IsUnique();
        });
    }

    private static void ConfigureEstadoPedido(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_ESTADO_PEDIDO>(builder =>
        {
            builder.ToTable("PB_ESTADO_PEDIDO");
            builder.HasKey(x => x.EPE_EstadoPedido);

            builder.Property(x => x.EPE_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.EPE_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.EPE_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.EPE_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.EPE_Codigo).IsUnique();
            builder.HasIndex(x => x.EPE_Nombre).IsUnique();
        });
    }

    private static void ConfigurePedido(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PEDIDO_CLIENTE>(builder =>
        {
            builder.ToTable("PB_PEDIDO_CLIENTE");
            builder.HasKey(x => x.PED_PedidoCliente);

            builder.Property(x => x.PED_Numero)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.PED_Fecha)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.PED_FechaVencimientoReserva)
                .HasColumnType("datetime2");

            builder.Property(x => x.PED_TipoCambio)
                .HasPrecision(18, 6)
                .HasDefaultValue(1m);

            builder.Property(x => x.PED_Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.PED_Descuento).HasPrecision(18, 2);
            builder.Property(x => x.PED_Impuesto).HasPrecision(18, 2);
            builder.Property(x => x.PED_Total).HasPrecision(18, 2);

            builder.Property(x => x.PED_Observaciones)
                .HasMaxLength(1000);

            builder.Property(x => x.PED_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.PED_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.PED_Numero).IsUnique();
            builder.HasIndex(x => x.PED_ClienteId);
            builder.HasIndex(x => x.PED_Fecha);
            builder.HasIndex(x => x.PED_EstadoPedidoId);

            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.PED_ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Sucursal)
                .WithMany()
                .HasForeignKey(x => x.PED_SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Almacen)
                .WithMany()
                .HasForeignKey(x => x.PED_AlmacenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Moneda)
                .WithMany(x => x.Pedidos)
                .HasForeignKey(x => x.PED_MonedaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.EstadoPedido)
                .WithMany(x => x.Pedidos)
                .HasForeignKey(x => x.PED_EstadoPedidoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.PED_EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_PB_PEDIDO_TIPO_CAMBIO",
                    "[PED_TipoCambio] > 0");

                table.HasCheckConstraint(
                    "CK_PB_PEDIDO_MONTOS",
                    "[PED_Subtotal] >= 0 AND [PED_Descuento] >= 0 " +
                    "AND [PED_Impuesto] >= 0 AND [PED_Total] >= 0");

                table.HasCheckConstraint(
                    "CK_PB_PEDIDO_FECHAS",
                    "[PED_FechaVencimientoReserva] IS NULL OR " +
                    "[PED_FechaVencimientoReserva] >= [PED_Fecha]");
            });
        });
    }

    private static void ConfigurePedidoDetalle(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PEDIDO_CLIENTE_DETALLE>(builder =>
        {
            builder.ToTable("PB_PEDIDO_CLIENTE_DETALLE");
            builder.HasKey(x => x.PDD_PedidoClienteDetalle);

            builder.Property(x => x.PDD_Cantidad).HasPrecision(18, 2);
            builder.Property(x => x.PDD_CantidadReservada).HasPrecision(18, 2);
            builder.Property(x => x.PDD_CantidadFacturada).HasPrecision(18, 2);
            builder.Property(x => x.PDD_PrecioUnitario).HasPrecision(18, 2);
            builder.Property(x => x.PDD_PorcentajeDescuento).HasPrecision(5, 2);
            builder.Property(x => x.PDD_MontoDescuento).HasPrecision(18, 2);
            builder.Property(x => x.PDD_PorcentajeImpuesto).HasPrecision(5, 2);
            builder.Property(x => x.PDD_MontoImpuesto).HasPrecision(18, 2);
            builder.Property(x => x.PDD_Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.PDD_Total).HasPrecision(18, 2);

            builder.HasIndex(x => new
            {
                x.PDD_PedidoClienteId,
                x.PDD_NumeroLinea
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.PDD_PedidoClienteId,
                x.PDD_LibroId
            }).IsUnique();

            builder.HasOne(x => x.PedidoCliente)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.PDD_PedidoClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Libro)
                .WithMany()
                .HasForeignKey(x => x.PDD_LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_PB_PEDIDO_DETALLE_CANTIDADES",
                    "[PDD_Cantidad] > 0 AND " +
                    "[PDD_CantidadReservada] >= 0 AND " +
                    "[PDD_CantidadFacturada] >= 0 AND " +
                    "[PDD_CantidadReservada] <= [PDD_Cantidad] AND " +
                    "[PDD_CantidadFacturada] <= [PDD_Cantidad]");

                table.HasCheckConstraint(
                    "CK_PB_PEDIDO_DETALLE_MONTOS",
                    "[PDD_PrecioUnitario] >= 0 AND " +
                    "[PDD_MontoDescuento] >= 0 AND " +
                    "[PDD_MontoImpuesto] >= 0 AND " +
                    "[PDD_Subtotal] >= 0 AND [PDD_Total] >= 0");
            });
        });
    }

    private static void ConfigureVenta(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_VENTA>(builder =>
        {
            builder.ToTable("PB_VENTA");
            builder.HasKey(x => x.VEN_Venta);

            builder.Property(x => x.VEN_Numero)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.VEN_Fecha)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.VEN_TipoCambio)
                .HasPrecision(18, 6)
                .HasDefaultValue(1m);

            builder.Property(x => x.VEN_Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.VEN_Descuento).HasPrecision(18, 2);
            builder.Property(x => x.VEN_Impuesto).HasPrecision(18, 2);
            builder.Property(x => x.VEN_Total).HasPrecision(18, 2);
            builder.Property(x => x.VEN_TotalPagado).HasPrecision(18, 2);
            builder.Property(x => x.VEN_CambioEntregado).HasPrecision(18, 2);

            builder.Property(x => x.VEN_FechaVencimientoCredito)
                .HasColumnType("date");

            builder.Property(x => x.VEN_Observaciones).HasMaxLength(1000);
            builder.Property(x => x.VEN_MotivoAnulacion).HasMaxLength(500);

            builder.Property(x => x.VEN_InventarioProcesado)
                .HasDefaultValue(false);

            builder.Property(x => x.VEN_Anulada)
                .HasDefaultValue(false);

            builder.Property(x => x.VEN_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.VEN_Numero).IsUnique();
            builder.HasIndex(x => x.VEN_Fecha);
            builder.HasIndex(x => x.VEN_ClienteId);
            builder.HasIndex(x => x.VEN_AperturaCajaId);

            builder.HasOne(x => x.PedidoCliente)
                .WithMany(x => x.Ventas)
                .HasForeignKey(x => x.VEN_PedidoClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.VEN_ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Sucursal)
                .WithMany()
                .HasForeignKey(x => x.VEN_SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Almacen)
                .WithMany()
                .HasForeignKey(x => x.VEN_AlmacenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.VEN_EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AperturaCaja)
                .WithMany(x => x.Ventas)
                .HasForeignKey(x => x.VEN_AperturaCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TipoVenta)
                .WithMany(x => x.Ventas)
                .HasForeignKey(x => x.VEN_TipoVentaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.EstadoVenta)
                .WithMany(x => x.Ventas)
                .HasForeignKey(x => x.VEN_EstadoVentaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Moneda)
                .WithMany(x => x.Ventas)
                .HasForeignKey(x => x.VEN_MonedaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_PB_VENTA_TIPO_CAMBIO",
                    "[VEN_TipoCambio] > 0");

                table.HasCheckConstraint(
                    "CK_PB_VENTA_MONTOS",
                    "[VEN_Subtotal] >= 0 AND [VEN_Descuento] >= 0 " +
                    "AND [VEN_Impuesto] >= 0 AND [VEN_Total] >= 0 " +
                    "AND [VEN_TotalPagado] >= 0 " +
                    "AND [VEN_CambioEntregado] >= 0");

                table.HasCheckConstraint(
                    "CK_PB_VENTA_ANULACION",
                    "[VEN_Anulada] = 0 OR " +
                    "([VEN_FechaAnulacion] IS NOT NULL AND " +
                    "[VEN_MotivoAnulacion] IS NOT NULL)");
            });
        });
    }

    private static void ConfigureVentaDetalle(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_VENTA_DETALLE>(builder =>
        {
            builder.ToTable("PB_VENTA_DETALLE");
            builder.HasKey(x => x.VDE_VentaDetalle);

            builder.Property(x => x.VDE_Cantidad).HasPrecision(18, 2);
            builder.Property(x => x.VDE_PrecioUnitario).HasPrecision(18, 2);
            builder.Property(x => x.VDE_CostoUnitario).HasPrecision(18, 4);
            builder.Property(x => x.VDE_PorcentajeDescuento).HasPrecision(5, 2);
            builder.Property(x => x.VDE_MontoDescuento).HasPrecision(18, 2);
            builder.Property(x => x.VDE_PorcentajeImpuesto).HasPrecision(5, 2);
            builder.Property(x => x.VDE_MontoImpuesto).HasPrecision(18, 2);
            builder.Property(x => x.VDE_Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.VDE_Total).HasPrecision(18, 2);
            builder.Property(x => x.VDE_CantidadDevuelta).HasPrecision(18, 2);

            builder.HasIndex(x => new
            {
                x.VDE_VentaId,
                x.VDE_NumeroLinea
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.VDE_VentaId,
                x.VDE_LibroId
            }).IsUnique();

            builder.HasOne(x => x.Venta)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.VDE_VentaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Libro)
                .WithMany()
                .HasForeignKey(x => x.VDE_LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_PB_VENTA_DETALLE_CANTIDAD",
                    "[VDE_Cantidad] > 0 AND " +
                    "[VDE_CantidadDevuelta] >= 0 AND " +
                    "[VDE_CantidadDevuelta] <= [VDE_Cantidad]");

                table.HasCheckConstraint(
                    "CK_PB_VENTA_DETALLE_MONTOS",
                    "[VDE_PrecioUnitario] >= 0 AND " +
                    "[VDE_CostoUnitario] >= 0 AND " +
                    "[VDE_MontoDescuento] >= 0 AND " +
                    "[VDE_MontoImpuesto] >= 0 AND " +
                    "[VDE_Subtotal] >= 0 AND [VDE_Total] >= 0");
            });
        });
    }

    private static void ConfigureVentaPago(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_VENTA_PAGO>(builder =>
        {
            builder.ToTable("PB_VENTA_PAGO");
            builder.HasKey(x => x.VPA_VentaPago);

            builder.Property(x => x.VPA_Fecha)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.VPA_TipoCambio).HasPrecision(18, 6);
            builder.Property(x => x.VPA_MontoMoneda).HasPrecision(18, 2);
            builder.Property(x => x.VPA_MontoBase).HasPrecision(18, 2);
            builder.Property(x => x.VPA_MontoRecibido).HasPrecision(18, 2);
            builder.Property(x => x.VPA_CambioEntregado).HasPrecision(18, 2);

            builder.Property(x => x.VPA_Referencia).HasMaxLength(150);
            builder.Property(x => x.VPA_Autorizacion).HasMaxLength(100);
            builder.Property(x => x.VPA_UltimosCuatroDigitos)
                .HasMaxLength(4)
                .IsUnicode(false);

            builder.Property(x => x.VPA_Anulado).HasDefaultValue(false);
            builder.Property(x => x.VPA_MotivoAnulacion).HasMaxLength(500);

            builder.HasIndex(x => x.VPA_VentaId);
            builder.HasIndex(x => x.VPA_MovimientoCajaId)
                .HasFilter("[VPA_MovimientoCajaId] IS NOT NULL");

            builder.HasOne(x => x.Venta)
                .WithMany(x => x.Pagos)
                .HasForeignKey(x => x.VPA_VentaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MetodoPago)
                .WithMany(x => x.PagosVenta)
                .HasForeignKey(x => x.VPA_MetodoPagoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Moneda)
                .WithMany(x => x.PagosVenta)
                .HasForeignKey(x => x.VPA_MonedaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MovimientoCaja)
                .WithMany(x => x.PagosVenta)
                .HasForeignKey(x => x.VPA_MovimientoCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_PB_VENTA_PAGO_MONTOS",
                    "[VPA_TipoCambio] > 0 AND " +
                    "[VPA_MontoMoneda] > 0 AND " +
                    "[VPA_MontoBase] > 0 AND " +
                    "[VPA_MontoRecibido] >= 0 AND " +
                    "[VPA_CambioEntregado] >= 0");
            });
        });
    }

    private static void ConfigureEstadoDevolucion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_ESTADO_DEVOLUCION>(builder =>
        {
            builder.ToTable("PB_ESTADO_DEVOLUCION");
            builder.HasKey(x => x.EDV_EstadoDevolucion);

            builder.Property(x => x.EDV_Codigo)
                .HasMaxLength(20).IsUnicode(false).IsRequired();

            builder.Property(x => x.EDV_Nombre)
                .HasMaxLength(100).IsRequired();

            builder.Property(x => x.EDV_Descripcion).HasMaxLength(500);
            builder.Property(x => x.EDV_Activo).HasDefaultValue(true);

            builder.HasIndex(x => x.EDV_Codigo).IsUnique();
            builder.HasIndex(x => x.EDV_Nombre).IsUnique();
        });
    }

    private static void ConfigureMotivoDevolucion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_MOTIVO_DEVOLUCION>(builder =>
        {
            builder.ToTable("PB_MOTIVO_DEVOLUCION");
            builder.HasKey(x => x.MDV_MotivoDevolucion);

            builder.Property(x => x.MDV_Codigo)
                .HasMaxLength(20).IsUnicode(false).IsRequired();

            builder.Property(x => x.MDV_Nombre)
                .HasMaxLength(150).IsRequired();

            builder.Property(x => x.MDV_Descripcion).HasMaxLength(500);
            builder.Property(x => x.MDV_Activo).HasDefaultValue(true);

            builder.HasIndex(x => x.MDV_Codigo).IsUnique();
            builder.HasIndex(x => x.MDV_Nombre).IsUnique();
        });
    }

    private static void ConfigureDevolucion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_DEVOLUCION_VENTA>(builder =>
        {
            builder.ToTable("PB_DEVOLUCION_VENTA");
            builder.HasKey(x => x.DEV_DevolucionVenta);

            builder.Property(x => x.DEV_Numero)
                .HasMaxLength(30).IsUnicode(false).IsRequired();

            builder.Property(x => x.DEV_Fecha)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.DEV_TipoCambio).HasPrecision(18, 6);
            builder.Property(x => x.DEV_Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.DEV_Impuesto).HasPrecision(18, 2);
            builder.Property(x => x.DEV_Total).HasPrecision(18, 2);

            builder.Property(x => x.DEV_Observaciones).HasMaxLength(1000);
            builder.Property(x => x.DEV_MotivoAnulacion).HasMaxLength(500);
            builder.Property(x => x.DEV_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.DEV_Numero).IsUnique();
            builder.HasIndex(x => x.DEV_VentaId);
            builder.HasIndex(x => x.DEV_Fecha);

            builder.HasOne(x => x.Venta)
                .WithMany(x => x.Devoluciones)
                .HasForeignKey(x => x.DEV_VentaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.EstadoDevolucion)
                .WithMany(x => x.Devoluciones)
                .HasForeignKey(x => x.DEV_EstadoDevolucionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Almacen)
                .WithMany()
                .HasForeignKey(x => x.DEV_AlmacenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.DEV_EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AperturaCaja)
                .WithMany(x => x.Devoluciones)
                .HasForeignKey(x => x.DEV_AperturaCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Moneda)
                .WithMany(x => x.Devoluciones)
                .HasForeignKey(x => x.DEV_MonedaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_PB_DEVOLUCION_MONTOS",
                    "[DEV_TipoCambio] > 0 AND [DEV_Subtotal] >= 0 " +
                    "AND [DEV_Impuesto] >= 0 AND [DEV_Total] >= 0");
            });
        });
    }

    private static void ConfigureDevolucionDetalle(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_DEVOLUCION_VENTA_DETALLE>(builder =>
        {
            builder.ToTable("PB_DEVOLUCION_VENTA_DETALLE");
            builder.HasKey(x => x.DVD_DevolucionVentaDetalle);

            builder.Property(x => x.DVD_Cantidad).HasPrecision(18, 2);
            builder.Property(x => x.DVD_PrecioUnitario).HasPrecision(18, 2);
            builder.Property(x => x.DVD_MontoImpuesto).HasPrecision(18, 2);
            builder.Property(x => x.DVD_Total).HasPrecision(18, 2);
            builder.Property(x => x.DVD_Observaciones).HasMaxLength(500);

            builder.HasIndex(x => new
            {
                x.DVD_DevolucionVentaId,
                x.DVD_VentaDetalleId
            }).IsUnique();

            builder.HasOne(x => x.DevolucionVenta)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.DVD_DevolucionVentaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.VentaDetalle)
                .WithMany(x => x.DevolucionesDetalles)
                .HasForeignKey(x => x.DVD_VentaDetalleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Libro)
                .WithMany()
                .HasForeignKey(x => x.DVD_LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MotivoDevolucion)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.DVD_MotivoDevolucionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_PB_DEVOLUCION_DETALLE_MONTOS",
                    "[DVD_Cantidad] > 0 AND " +
                    "[DVD_PrecioUnitario] >= 0 AND " +
                    "[DVD_MontoImpuesto] >= 0 AND [DVD_Total] >= 0");
            });
        });
    }

    private static void ConfigureDevolucionPago(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_DEVOLUCION_PAGO>(builder =>
        {
            builder.ToTable("PB_DEVOLUCION_PAGO");
            builder.HasKey(x => x.DPA_DevolucionPago);

            builder.Property(x => x.DPA_Fecha)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.DPA_TipoCambio).HasPrecision(18, 6);
            builder.Property(x => x.DPA_MontoMoneda).HasPrecision(18, 2);
            builder.Property(x => x.DPA_MontoBase).HasPrecision(18, 2);
            builder.Property(x => x.DPA_Referencia).HasMaxLength(150);
            builder.Property(x => x.DPA_Autorizacion).HasMaxLength(100);

            builder.HasOne(x => x.DevolucionVenta)
                .WithMany(x => x.Reembolsos)
                .HasForeignKey(x => x.DPA_DevolucionVentaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MetodoPago)
                .WithMany(x => x.PagosDevolucion)
                .HasForeignKey(x => x.DPA_MetodoPagoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Moneda)
                .WithMany(x => x.PagosDevolucion)
                .HasForeignKey(x => x.DPA_MonedaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MovimientoCaja)
                .WithMany(x => x.PagosDevolucion)
                .HasForeignKey(x => x.DPA_MovimientoCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_PB_DEVOLUCION_PAGO_MONTOS",
                    "[DPA_TipoCambio] > 0 AND " +
                    "[DPA_MontoMoneda] > 0 AND [DPA_MontoBase] > 0");
            });
        });
    }
}