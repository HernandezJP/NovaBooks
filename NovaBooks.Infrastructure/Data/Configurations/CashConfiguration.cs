using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Cash;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class CashConfiguration
{
    public static void ConfigureCash(this ModelBuilder modelBuilder)
    {
        ConfigureCaja(modelBuilder);
        ConfigureAperturaCaja(modelBuilder);
        ConfigureCierreCaja(modelBuilder);
        ConfigureCierreCajaDetalle(modelBuilder);
        ConfigureTipoMovimientoCaja(modelBuilder);
        ConfigureMovimientoCaja(modelBuilder);
    }

    private static void ConfigureCaja(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_CAJA>(builder =>
        {
            builder.ToTable("PB_CAJA");

            builder.HasKey(x => x.CAJ_Caja);

            builder.Property(x => x.CAJ_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.CAJ_Nombre)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.CAJ_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.CAJ_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.CAJ_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => new
            {
                x.CAJ_SucursalId,
                x.CAJ_Codigo
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.CAJ_SucursalId,
                x.CAJ_Nombre
            }).IsUnique();

            builder.HasOne(x => x.Sucursal)
                .WithMany()
                .HasForeignKey(x => x.CAJ_SucursalId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAperturaCaja(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_APERTURA_CAJA>(builder =>
        {
            builder.ToTable("PB_APERTURA_CAJA");

            builder.HasKey(x => x.ACA_AperturaCaja);

            builder.Property(x => x.ACA_FechaApertura)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.ACA_MontoInicial)
                .HasPrecision(18, 2);

            builder.Property(x => x.ACA_Observaciones)
                .HasMaxLength(1000);

            builder.Property(x => x.ACA_Abierta)
                .HasDefaultValue(true);

            builder.Property(x => x.ACA_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.ACA_FechaApertura);

            builder.HasIndex(x => new
            {
                x.ACA_CajaId,
                x.ACA_Abierta
            })
            .IsUnique()
            .HasFilter("[ACA_Abierta] = 1");

            builder.HasOne(x => x.Caja)
                .WithMany(x => x.Aperturas)
                .HasForeignKey(x => x.ACA_CajaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.ACA_EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Moneda)
                .WithMany(x => x.AperturasCaja)
                .HasForeignKey(x => x.ACA_MonedaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_APERTURA_CAJA_MONTO",
                    "[ACA_MontoInicial] >= 0");
            });
        });
    }

    private static void ConfigureCierreCaja(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_CIERRE_CAJA>(builder =>
        {
            builder.ToTable("PB_CIERRE_CAJA");

            builder.HasKey(x => x.CCA_CierreCaja);

            builder.Property(x => x.CCA_FechaCierre)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.CCA_MontoInicial)
                .HasPrecision(18, 2);

            builder.Property(x => x.CCA_TotalIngresosEfectivo)
                .HasPrecision(18, 2);

            builder.Property(x => x.CCA_TotalEgresosEfectivo)
                .HasPrecision(18, 2);

            builder.Property(x => x.CCA_MontoEsperado)
                .HasPrecision(18, 2);

            builder.Property(x => x.CCA_MontoContado)
                .HasPrecision(18, 2);

            builder.Property(x => x.CCA_Diferencia)
                .HasPrecision(18, 2);

            builder.Property(x => x.CCA_Observaciones)
                .HasMaxLength(1000);

            builder.Property(x => x.CCA_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.CCA_AperturaCajaId)
                .IsUnique();

            builder.HasIndex(x => x.CCA_FechaCierre);

            builder.HasOne(x => x.AperturaCaja)
                .WithOne(x => x.CierreCaja)
                .HasForeignKey<PB_CIERRE_CAJA>(
                    x => x.CCA_AperturaCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.CCA_EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_CIERRE_CAJA_MONTOS",
                    "[CCA_MontoInicial] >= 0 AND " +
                    "[CCA_TotalIngresosEfectivo] >= 0 AND " +
                    "[CCA_TotalEgresosEfectivo] >= 0 AND " +
                    "[CCA_MontoEsperado] >= 0 AND " +
                    "[CCA_MontoContado] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_CIERRE_CAJA_DIFERENCIA",
                    "[CCA_Diferencia] = " +
                    "[CCA_MontoContado] - [CCA_MontoEsperado]");
            });
        });
    }

    private static void ConfigureCierreCajaDetalle(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_CIERRE_CAJA_DETALLE>(builder =>
        {
            builder.ToTable("PB_CIERRE_CAJA_DETALLE");

            builder.HasKey(x => x.CCD_CierreCajaDetalle);

            builder.Property(x => x.CCD_MontoSistema)
                .HasPrecision(18, 2);

            builder.Property(x => x.CCD_MontoContado)
                .HasPrecision(18, 2);

            builder.Property(x => x.CCD_Diferencia)
                .HasPrecision(18, 2);

            builder.Property(x => x.CCD_Observaciones)
                .HasMaxLength(500);

            builder.HasIndex(x => new
            {
                x.CCD_CierreCajaId,
                x.CCD_MetodoPagoId
            }).IsUnique();

            builder.HasOne(x => x.CierreCaja)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.CCD_CierreCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MetodoPago)
                .WithMany(x => x.CierresCajaDetalles)
                .HasForeignKey(x => x.CCD_MetodoPagoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_CIERRE_DETALLE_MONTOS",
                    "[CCD_MontoSistema] >= 0 AND " +
                    "[CCD_MontoContado] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_CIERRE_DETALLE_DIFERENCIA",
                    "[CCD_Diferencia] = " +
                    "[CCD_MontoContado] - [CCD_MontoSistema]");
            });
        });
    }

    private static void ConfigureTipoMovimientoCaja(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_TIPO_MOVIMIENTO_CAJA>(builder =>
        {
            builder.ToTable("PB_TIPO_MOVIMIENTO_CAJA");

            builder.HasKey(x => x.TMC_TipoMovimientoCaja);

            builder.Property(x => x.TMC_Codigo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.TMC_Nombre)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.TMC_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.TMC_RequiereAutorizacion)
                .HasDefaultValue(false);

            builder.Property(x => x.TMC_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.TMC_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.TMC_Nombre)
                .IsUnique();

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_TIPO_MOVIMIENTO_CAJA_NATURALEZA",
                    "[TMC_Naturaleza] IN (-1, 1)");
            });
        });
    }

    private static void ConfigureMovimientoCaja(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_MOVIMIENTO_CAJA>(builder =>
        {
            builder.ToTable("PB_MOVIMIENTO_CAJA");

            builder.HasKey(x => x.MCA_MovimientoCaja);

            builder.Property(x => x.MCA_Numero)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.MCA_Fecha)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.MCA_TipoCambio)
                .HasPrecision(18, 6)
                .HasDefaultValue(1m);

            builder.Property(x => x.MCA_MontoMoneda)
                .HasPrecision(18, 2);

            builder.Property(x => x.MCA_MontoBase)
                .HasPrecision(18, 2);

            builder.Property(x => x.MCA_Referencia)
                .HasMaxLength(150);

            builder.Property(x => x.MCA_Concepto)
                .HasMaxLength(500);

            builder.Property(x => x.MCA_Anulado)
                .HasDefaultValue(false);

            builder.Property(x => x.MCA_MotivoAnulacion)
                .HasMaxLength(500);

            builder.Property(x => x.MCA_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.MCA_Numero)
                .IsUnique();

            builder.HasIndex(x => new
            {
                x.MCA_AperturaCajaId,
                x.MCA_Fecha
            });

            builder.HasIndex(x => x.MCA_VentaId)
                .HasFilter("[MCA_VentaId] IS NOT NULL");

            builder.HasIndex(x => x.MCA_DevolucionVentaId)
                .HasFilter("[MCA_DevolucionVentaId] IS NOT NULL");

            builder.HasOne(x => x.AperturaCaja)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.MCA_AperturaCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TipoMovimientoCaja)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.MCA_TipoMovimientoCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MetodoPago)
                .WithMany(x => x.MovimientosCaja)
                .HasForeignKey(x => x.MCA_MetodoPagoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Moneda)
                .WithMany(x => x.MovimientosCaja)
                .HasForeignKey(x => x.MCA_MonedaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.MCA_EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Venta)
                .WithMany()
                .HasForeignKey(x => x.MCA_VentaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.DevolucionVenta)
                .WithMany()
                .HasForeignKey(x => x.MCA_DevolucionVentaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_MOVIMIENTO_CAJA_MONTOS",
                    "[MCA_TipoCambio] > 0 AND " +
                    "[MCA_MontoMoneda] > 0 AND " +
                    "[MCA_MontoBase] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_MOVIMIENTO_CAJA_REFERENCIA",
                    "NOT ([MCA_VentaId] IS NOT NULL AND " +
                    "[MCA_DevolucionVentaId] IS NOT NULL)");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_MOVIMIENTO_CAJA_ANULACION",
                    "[MCA_Anulado] = 0 OR " +
                    "([MCA_FechaAnulacion] IS NOT NULL AND " +
                    "[MCA_MotivoAnulacion] IS NOT NULL)");
            });
        });
    }
}