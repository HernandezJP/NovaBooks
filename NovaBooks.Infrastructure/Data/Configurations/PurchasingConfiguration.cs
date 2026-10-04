using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Purchasing;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class PurchasingConfiguration
{
    public static void ConfigurePurchasing(
        this ModelBuilder modelBuilder)
    {
        ConfigureEstadoOrdenCompra(modelBuilder);
        ConfigureOrdenCompra(modelBuilder);
        ConfigureOrdenCompraDetalle(modelBuilder);
        ConfigureEstadoRecepcionCompra(modelBuilder);
        ConfigureRecepcionCompra(modelBuilder);
        ConfigureRecepcionCompraDetalle(modelBuilder);
    }

    private static void ConfigureEstadoOrdenCompra(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_ESTADO_ORDEN_COMPRA>(builder =>
        {
            builder.ToTable("PB_ESTADO_ORDEN_COMPRA");

            builder.HasKey(x => x.EOC_EstadoOrdenCompra);

            builder.Property(x => x.EOC_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.EOC_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.EOC_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.EOC_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.EOC_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.EOC_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigureOrdenCompra(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_ORDEN_COMPRA>(builder =>
        {
            builder.ToTable("PB_ORDEN_COMPRA");

            builder.HasKey(x => x.OCO_OrdenCompra);

            builder.Property(x => x.OCO_Numero)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.OCO_FechaEmision)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.OCO_FechaEntregaEsperada)
                .HasColumnType("date");

            builder.Property(x => x.OCO_TipoCambio)
                .HasPrecision(18, 6)
                .HasDefaultValue(1m);

            builder.Property(x => x.OCO_Subtotal)
                .HasPrecision(18, 2);

            builder.Property(x => x.OCO_Descuento)
                .HasPrecision(18, 2);

            builder.Property(x => x.OCO_Impuesto)
                .HasPrecision(18, 2);

            builder.Property(x => x.OCO_Total)
                .HasPrecision(18, 2);

            builder.Property(x => x.OCO_Observaciones)
                .HasMaxLength(1000);

            builder.Property(x => x.OCO_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.OCO_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.OCO_Numero)
                .IsUnique();

            builder.HasIndex(x => x.OCO_ProveedorId);

            builder.HasIndex(x => x.OCO_SucursalId);

            builder.HasIndex(x => x.OCO_EstadoOrdenCompraId);

            builder.HasIndex(x => x.OCO_FechaEmision);

            builder.HasOne(x => x.Proveedor)
                .WithMany()
                .HasForeignKey(x => x.OCO_ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Sucursal)
                .WithMany()
                .HasForeignKey(x => x.OCO_SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Moneda)
                .WithMany(x => x.OrdenesCompra)
                .HasForeignKey(x => x.OCO_MonedaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.EstadoOrdenCompra)
                .WithMany(x => x.OrdenesCompra)
                .HasForeignKey(x => x.OCO_EstadoOrdenCompraId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.EmpleadoSolicitante)
                .WithMany()
                .HasForeignKey(x => x.OCO_EmpleadoSolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_ORDEN_COMPRA_TIPO_CAMBIO",
                    "[OCO_TipoCambio] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_ORDEN_COMPRA_MONTOS",
                    "[OCO_Subtotal] >= 0 AND " +
                    "[OCO_Descuento] >= 0 AND " +
                    "[OCO_Impuesto] >= 0 AND " +
                    "[OCO_Total] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_ORDEN_COMPRA_DESCUENTO",
                    "[OCO_Descuento] <= [OCO_Subtotal]");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_ORDEN_COMPRA_FECHA_ENTREGA",
                    "[OCO_FechaEntregaEsperada] IS NULL OR " +
                    "[OCO_FechaEntregaEsperada] >= " +
                    "CAST([OCO_FechaEmision] AS date)");
            });
        });
    }

    private static void ConfigureOrdenCompraDetalle(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_ORDEN_COMPRA_DETALLE>(builder =>
        {
            builder.ToTable("PB_ORDEN_COMPRA_DETALLE");

            builder.HasKey(x => x.OCD_OrdenCompraDetalle);

            builder.Property(x => x.OCD_CantidadSolicitada)
                .HasPrecision(18, 2);

            builder.Property(x => x.OCD_CantidadRecibida)
                .HasPrecision(18, 2);

            builder.Property(x => x.OCD_CostoUnitario)
                .HasPrecision(18, 4);

            builder.Property(x => x.OCD_PorcentajeDescuento)
                .HasPrecision(5, 2);

            builder.Property(x => x.OCD_MontoDescuento)
                .HasPrecision(18, 2);

            builder.Property(x => x.OCD_PorcentajeImpuesto)
                .HasPrecision(5, 2);

            builder.Property(x => x.OCD_MontoImpuesto)
                .HasPrecision(18, 2);

            builder.Property(x => x.OCD_Subtotal)
                .HasPrecision(18, 2);

            builder.Property(x => x.OCD_Total)
                .HasPrecision(18, 2);

            builder.Property(x => x.OCD_Observaciones)
                .HasMaxLength(500);

            builder.HasIndex(x => new
            {
                x.OCD_OrdenCompraId,
                x.OCD_LibroId
            }).IsUnique();

            builder.HasOne(x => x.OrdenCompra)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.OCD_OrdenCompraId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Libro)
                .WithMany()
                .HasForeignKey(x => x.OCD_LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_ORDEN_COMPRA_DETALLE_CANTIDADES",
                    "[OCD_CantidadSolicitada] > 0 AND " +
                    "[OCD_CantidadRecibida] >= 0 AND " +
                    "[OCD_CantidadRecibida] <= " +
                    "[OCD_CantidadSolicitada]");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_ORDEN_COMPRA_DETALLE_COSTO",
                    "[OCD_CostoUnitario] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_ORDEN_COMPRA_DETALLE_DESCUENTO",
                    "[OCD_PorcentajeDescuento] >= 0 AND " +
                    "[OCD_PorcentajeDescuento] <= 100 AND " +
                    "[OCD_MontoDescuento] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_ORDEN_COMPRA_DETALLE_IMPUESTO",
                    "[OCD_PorcentajeImpuesto] >= 0 AND " +
                    "[OCD_PorcentajeImpuesto] <= 100 AND " +
                    "[OCD_MontoImpuesto] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_ORDEN_COMPRA_DETALLE_TOTALES",
                    "[OCD_Subtotal] >= 0 AND [OCD_Total] >= 0");
            });
        });
    }

    private static void ConfigureEstadoRecepcionCompra(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_ESTADO_RECEPCION_COMPRA>(builder =>
        {
            builder.ToTable("PB_ESTADO_RECEPCION_COMPRA");

            builder.HasKey(x => x.ERC_EstadoRecepcionCompra);

            builder.Property(x => x.ERC_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.ERC_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.ERC_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.ERC_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.ERC_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.ERC_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigureRecepcionCompra(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_RECEPCION_COMPRA>(builder =>
        {
            builder.ToTable("PB_RECEPCION_COMPRA");

            builder.HasKey(x => x.REC_RecepcionCompra);

            builder.Property(x => x.REC_Numero)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.REC_FechaRecepcion)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.REC_NumeroDocumentoProveedor)
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.Property(x => x.REC_SerieDocumentoProveedor)
                .HasMaxLength(30)
                .IsUnicode(false);

            builder.Property(x => x.REC_Observaciones)
                .HasMaxLength(1000);

            builder.Property(x => x.REC_ActualizaInventario)
                .HasDefaultValue(false);

            builder.Property(x => x.REC_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.REC_Numero)
                .IsUnique();

            builder.HasIndex(x => x.REC_OrdenCompraId);

            builder.HasIndex(x => x.REC_AlmacenId);

            builder.HasIndex(x => x.REC_EstadoRecepcionCompraId);

            builder.HasIndex(x => x.REC_FechaRecepcion);

            builder.HasIndex(x => new
            {
                x.REC_SerieDocumentoProveedor,
                x.REC_NumeroDocumentoProveedor
            })
            .HasFilter(
                "[REC_NumeroDocumentoProveedor] IS NOT NULL");

            builder.HasOne(x => x.OrdenCompra)
                .WithMany(x => x.Recepciones)
                .HasForeignKey(x => x.REC_OrdenCompraId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Almacen)
                .WithMany()
                .HasForeignKey(x => x.REC_AlmacenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.EstadoRecepcionCompra)
                .WithMany(x => x.Recepciones)
                .HasForeignKey(x => x.REC_EstadoRecepcionCompraId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.REC_EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRecepcionCompraDetalle(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_RECEPCION_COMPRA_DETALLE>(builder =>
        {
            builder.ToTable("PB_RECEPCION_COMPRA_DETALLE");

            builder.HasKey(x => x.RCD_RecepcionCompraDetalle);

            builder.Property(x => x.RCD_CantidadRecibida)
                .HasPrecision(18, 2);

            builder.Property(x => x.RCD_CantidadAceptada)
                .HasPrecision(18, 2);

            builder.Property(x => x.RCD_CantidadRechazada)
                .HasPrecision(18, 2);

            builder.Property(x => x.RCD_CostoUnitario)
                .HasPrecision(18, 4);

            builder.Property(x => x.RCD_MotivoRechazo)
                .HasMaxLength(500);

            builder.Property(x => x.RCD_Observaciones)
                .HasMaxLength(500);

            builder.HasIndex(x => new
            {
                x.RCD_RecepcionCompraId,
                x.RCD_OrdenCompraDetalleId
            }).IsUnique();

            builder.HasIndex(x => x.RCD_LibroId);

            builder.HasIndex(x => x.RCD_LoteId);

            builder.HasOne(x => x.RecepcionCompra)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.RCD_RecepcionCompraId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.OrdenCompraDetalle)
                .WithMany(x => x.RecepcionesDetalles)
                .HasForeignKey(x => x.RCD_OrdenCompraDetalleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Libro)
                .WithMany()
                .HasForeignKey(x => x.RCD_LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Lote)
                .WithMany()
                .HasForeignKey(x => x.RCD_LoteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_RECEPCION_DETALLE_CANTIDADES",
                    "[RCD_CantidadRecibida] > 0 AND " +
                    "[RCD_CantidadAceptada] >= 0 AND " +
                    "[RCD_CantidadRechazada] >= 0 AND " +
                    "[RCD_CantidadRecibida] = " +
                    "[RCD_CantidadAceptada] + " +
                    "[RCD_CantidadRechazada]");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_RECEPCION_DETALLE_COSTO",
                    "[RCD_CostoUnitario] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_RECEPCION_DETALLE_RECHAZO",
                    "[RCD_CantidadRechazada] = 0 OR " +
                    "([RCD_MotivoRechazo] IS NOT NULL AND " +
                    "LEN(LTRIM(RTRIM(" +
                    "[RCD_MotivoRechazo]))) > 0)");
            });
        });
    }
}