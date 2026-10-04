using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Inventory;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class InventoryConfiguration
{
    public static void ConfigureInventory(this ModelBuilder modelBuilder)
    {
        ConfigureAlmacen(modelBuilder);
        ConfigureExistencia(modelBuilder);
        ConfigureLote(modelBuilder);
        ConfigureTipoMovimiento(modelBuilder);
        ConfigureMovimiento(modelBuilder);
        ConfigureMovimientoDetalle(modelBuilder);
        ConfigureMotivoAjuste(modelBuilder);
        ConfigureAjuste(modelBuilder);
        ConfigureAjusteDetalle(modelBuilder);
    }

    private static void ConfigureAlmacen(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_ALMACEN>(builder =>
        {
            builder.ToTable("PB_ALMACEN");

            builder.HasKey(x => x.ALM_Almacen);

            builder.Property(x => x.ALM_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.ALM_Nombre)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.ALM_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.ALM_EsPrincipal)
                .HasDefaultValue(false);

            builder.Property(x => x.ALM_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.ALM_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => new
            {
                x.ALM_SucursalId,
                x.ALM_Codigo
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.ALM_SucursalId,
                x.ALM_Nombre
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.ALM_SucursalId,
                x.ALM_EsPrincipal
            })
            .IsUnique()
            .HasFilter(
                "[ALM_EsPrincipal] = 1 AND [ALM_Activo] = 1");

            builder.HasOne(x => x.Sucursal)
                .WithMany()
                .HasForeignKey(x => x.ALM_SucursalId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureExistencia(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_EXISTENCIA>(builder =>
        {
            builder.ToTable("PB_EXISTENCIA");

            builder.HasKey(x => x.EXI_Existencia);

            builder.Property(x => x.EXI_CantidadDisponible)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(x => x.EXI_CantidadReservada)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(x => x.EXI_StockMinimo)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(x => x.EXI_StockMaximo)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(x => x.EXI_Ubicacion)
                .HasMaxLength(100);

            builder.Property(x => x.EXI_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.EXI_Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            builder.HasIndex(x => new
            {
                x.EXI_AlmacenId,
                x.EXI_LibroId
            }).IsUnique();

            builder.HasOne(x => x.Almacen)
                .WithMany(x => x.Existencias)
                .HasForeignKey(x => x.EXI_AlmacenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Libro)
                .WithMany()
                .HasForeignKey(x => x.EXI_LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_EXISTENCIA_CANTIDADES",
                    "[EXI_CantidadDisponible] >= 0 AND " +
                    "[EXI_CantidadReservada] >= 0 AND " +
                    "[EXI_CantidadReservada] <= " +
                    "[EXI_CantidadDisponible]");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_EXISTENCIA_STOCK",
                    "[EXI_StockMinimo] >= 0 AND " +
                    "[EXI_StockMaximo] >= 0 AND " +
                    "([EXI_StockMaximo] = 0 OR " +
                    "[EXI_StockMaximo] >= [EXI_StockMinimo])");
            });
        });
    }

    private static void ConfigureLote(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_LOTE>(builder =>
        {
            builder.ToTable("PB_LOTE");

            builder.HasKey(x => x.LOT_Lote);

            builder.Property(x => x.LOT_Numero)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.LOT_FechaIngreso)
                .HasColumnType("date");

            builder.Property(x => x.LOT_FechaVencimiento)
                .HasColumnType("date");

            builder.Property(x => x.LOT_CantidadInicial)
                .HasPrecision(18, 2);

            builder.Property(x => x.LOT_CantidadActual)
                .HasPrecision(18, 2);

            builder.Property(x => x.LOT_CostoUnitario)
                .HasPrecision(18, 4);

            builder.Property(x => x.LOT_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.LOT_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => new
            {
                x.LOT_AlmacenId,
                x.LOT_LibroId,
                x.LOT_Numero
            }).IsUnique();

            builder.HasOne(x => x.Almacen)
                .WithMany(x => x.Lotes)
                .HasForeignKey(x => x.LOT_AlmacenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Libro)
                .WithMany()
                .HasForeignKey(x => x.LOT_LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Proveedor)
                .WithMany()
                .HasForeignKey(x => x.LOT_ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_LOTE_CANTIDADES",
                    "[LOT_CantidadInicial] > 0 AND " +
                    "[LOT_CantidadActual] >= 0 AND " +
                    "[LOT_CantidadActual] <= [LOT_CantidadInicial]");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_LOTE_COSTO",
                    "[LOT_CostoUnitario] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_LOTE_FECHAS",
                    "[LOT_FechaVencimiento] IS NULL OR " +
                    "[LOT_FechaVencimiento] >= [LOT_FechaIngreso]");
            });
        });
    }

    private static void ConfigureTipoMovimiento(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_TIPO_MOVIMIENTO_INVENTARIO>(builder =>
        {
            builder.ToTable("PB_TIPO_MOVIMIENTO_INVENTARIO");

            builder.HasKey(x => x.TMI_TipoMovimientoInventario);

            builder.Property(x => x.TMI_Codigo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.TMI_Nombre)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.TMI_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.TMI_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.TMI_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.TMI_Nombre)
                .IsUnique();

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_TIPO_MOVIMIENTO_NATURALEZA",
                    "[TMI_Naturaleza] IN (-1, 0, 1)");
            });
        });
    }

    private static void ConfigureMovimiento(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_MOVIMIENTO_INVENTARIO>(builder =>
        {
            builder.ToTable("PB_MOVIMIENTO_INVENTARIO");

            builder.HasKey(x => x.MOV_MovimientoInventario);

            builder.Property(x => x.MOV_Numero)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.MOV_Fecha)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.MOV_DocumentoReferencia)
                .HasMaxLength(100);

            builder.Property(x => x.MOV_Observaciones)
                .HasMaxLength(1000);

            builder.Property(x => x.MOV_Procesado)
                .HasDefaultValue(false);

            builder.Property(x => x.MOV_Anulado)
                .HasDefaultValue(false);

            builder.Property(x => x.MOV_MotivoAnulacion)
                .HasMaxLength(500);

            builder.Property(x => x.MOV_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.MOV_Numero)
                .IsUnique();

            builder.HasIndex(x => x.MOV_Fecha);

            builder.HasIndex(x => x.MOV_TipoMovimientoId);

            builder.HasIndex(x => x.MOV_RecepcionCompraId)
                .HasFilter("[MOV_RecepcionCompraId] IS NOT NULL");

            builder.HasOne(x => x.TipoMovimiento)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.MOV_TipoMovimientoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AlmacenOrigen)
                .WithMany()
                .HasForeignKey(x => x.MOV_AlmacenOrigenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AlmacenDestino)
                .WithMany()
                .HasForeignKey(x => x.MOV_AlmacenDestinoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.RecepcionCompra)
                .WithMany(x => x.MovimientosInventario)
                .HasForeignKey(x => x.MOV_RecepcionCompraId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.MOV_EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_MOVIMIENTO_ALMACENES",
                    "[MOV_AlmacenOrigenId] IS NOT NULL OR " +
                    "[MOV_AlmacenDestinoId] IS NOT NULL");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_MOVIMIENTO_ALMACENES_DIFERENTES",
                    "[MOV_AlmacenOrigenId] IS NULL OR " +
                    "[MOV_AlmacenDestinoId] IS NULL OR " +
                    "[MOV_AlmacenOrigenId] <> " +
                    "[MOV_AlmacenDestinoId]");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_MOVIMIENTO_ANULACION",
                    "[MOV_Anulado] = 0 OR " +
                    "([MOV_FechaAnulacion] IS NOT NULL AND " +
                    "[MOV_MotivoAnulacion] IS NOT NULL)");
            });
        });
    }

    private static void ConfigureMovimientoDetalle(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_MOVIMIENTO_INVENTARIO_DETALLE>(
            builder =>
            {
                builder.ToTable(
                    "PB_MOVIMIENTO_INVENTARIO_DETALLE");

                builder.HasKey(
                    x => x.MDE_MovimientoInventarioDetalle);

                builder.Property(x => x.MDE_Cantidad)
                    .HasPrecision(18, 2);

                builder.Property(x => x.MDE_CostoUnitario)
                    .HasPrecision(18, 4);

                builder.Property(x => x.MDE_CostoTotal)
                    .HasPrecision(18, 2);

                builder.Property(x => x.MDE_Observaciones)
                    .HasMaxLength(500);

                builder.HasIndex(x => new
                {
                    x.MDE_MovimientoInventarioId,
                    x.MDE_LibroId,
                    x.MDE_LoteId
                });

                builder.HasOne(x => x.MovimientoInventario)
                    .WithMany(x => x.Detalles)
                    .HasForeignKey(
                        x => x.MDE_MovimientoInventarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(x => x.Libro)
                    .WithMany()
                    .HasForeignKey(x => x.MDE_LibroId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(x => x.Lote)
                    .WithMany(x => x.MovimientosDetalles)
                    .HasForeignKey(x => x.MDE_LoteId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_PB_MOVIMIENTO_DETALLE_CANTIDAD",
                        "[MDE_Cantidad] > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_PB_MOVIMIENTO_DETALLE_COSTOS",
                        "[MDE_CostoUnitario] >= 0 AND " +
                        "[MDE_CostoTotal] >= 0");
                });
            });
    }

    private static void ConfigureMotivoAjuste(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_MOTIVO_AJUSTE>(builder =>
        {
            builder.ToTable("PB_MOTIVO_AJUSTE");

            builder.HasKey(x => x.MAJ_MotivoAjuste);

            builder.Property(x => x.MAJ_Codigo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.MAJ_Nombre)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.MAJ_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.MAJ_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.MAJ_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.MAJ_Nombre)
                .IsUnique();

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_MOTIVO_AJUSTE_NATURALEZA",
                    "[MAJ_Naturaleza] IN (-1, 1)");
            });
        });
    }

    private static void ConfigureAjuste(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_AJUSTE_INVENTARIO>(builder =>
        {
            builder.ToTable("PB_AJUSTE_INVENTARIO");

            builder.HasKey(x => x.AJU_AjusteInventario);

            builder.Property(x => x.AJU_Numero)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.AJU_Fecha)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.AJU_Observaciones)
                .HasMaxLength(1000);

            builder.Property(x => x.AJU_Procesado)
                .HasDefaultValue(false);

            builder.Property(x => x.AJU_Anulado)
                .HasDefaultValue(false);

            builder.Property(x => x.AJU_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.AJU_Numero)
                .IsUnique();

            builder.HasIndex(x => x.AJU_MovimientoInventarioId)
                .IsUnique()
                .HasFilter(
                    "[AJU_MovimientoInventarioId] IS NOT NULL");

            builder.HasOne(x => x.Almacen)
                .WithMany()
                .HasForeignKey(x => x.AJU_AlmacenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MotivoAjuste)
                .WithMany(x => x.Ajustes)
                .HasForeignKey(x => x.AJU_MotivoAjusteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MovimientoInventario)
                .WithOne()
                .HasForeignKey<PB_AJUSTE_INVENTARIO>(
                    x => x.AJU_MovimientoInventarioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.AJU_EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAjusteDetalle(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_AJUSTE_INVENTARIO_DETALLE>(
            builder =>
            {
                builder.ToTable(
                    "PB_AJUSTE_INVENTARIO_DETALLE");

                builder.HasKey(
                    x => x.AID_AjusteInventarioDetalle);

                builder.Property(x => x.AID_CantidadSistema)
                    .HasPrecision(18, 2);

                builder.Property(x => x.AID_CantidadFisica)
                    .HasPrecision(18, 2);

                builder.Property(x => x.AID_Diferencia)
                    .HasPrecision(18, 2);

                builder.Property(x => x.AID_CostoUnitario)
                    .HasPrecision(18, 4);

                builder.Property(x => x.AID_Observaciones)
                    .HasMaxLength(500);

                builder.HasIndex(x => new
                {
                    x.AID_AjusteInventarioId,
                    x.AID_LibroId
                }).IsUnique();

                builder.HasOne(x => x.AjusteInventario)
                    .WithMany(x => x.Detalles)
                    .HasForeignKey(
                        x => x.AID_AjusteInventarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(x => x.Libro)
                    .WithMany()
                    .HasForeignKey(x => x.AID_LibroId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_PB_AJUSTE_DETALLE_CANTIDADES",
                        "[AID_CantidadSistema] >= 0 AND " +
                        "[AID_CantidadFisica] >= 0 AND " +
                        "[AID_Diferencia] = " +
                        "[AID_CantidadFisica] - " +
                        "[AID_CantidadSistema]");

                    tableBuilder.HasCheckConstraint(
                        "CK_PB_AJUSTE_DETALLE_COSTO",
                        "[AID_CostoUnitario] >= 0");
                });
            });
    }
}