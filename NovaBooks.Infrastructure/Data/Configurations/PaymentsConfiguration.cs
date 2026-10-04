using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Payments;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class PaymentsConfiguration
{
    public static void ConfigurePayments(this ModelBuilder modelBuilder)
    {
        ConfigureMetodoPago(modelBuilder);
        ConfigureMoneda(modelBuilder);
    }

    private static void ConfigureMetodoPago(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_METODO_PAGO>(builder =>
        {
            builder.ToTable("PB_METODO_PAGO");

            builder.HasKey(x => x.MPA_MetodoPago);

            builder.Property(x => x.MPA_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.MPA_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.MPA_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.MPA_RequiereReferencia)
                .HasDefaultValue(false);

            builder.Property(x => x.MPA_RequiereAutorizacion)
                .HasDefaultValue(false);

            builder.Property(x => x.MPA_AfectaEfectivo)
                .HasDefaultValue(false);

            builder.Property(x => x.MPA_PermiteCambio)
                .HasDefaultValue(false);

            builder.Property(x => x.MPA_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.MPA_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.MPA_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.MPA_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigureMoneda(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_MONEDA>(builder =>
        {
            builder.ToTable("PB_MONEDA");

            builder.HasKey(x => x.MON_Moneda);

            builder.Property(x => x.MON_Codigo)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.MON_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.MON_Simbolo)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(x => x.MON_Decimales)
                .HasDefaultValue(2);

            builder.Property(x => x.MON_EsPredeterminada)
                .HasDefaultValue(false);

            builder.Property(x => x.MON_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.MON_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.MON_Nombre)
                .IsUnique();

            builder.HasIndex(x => x.MON_EsPredeterminada)
                .IsUnique()
                .HasFilter(
                    "[MON_EsPredeterminada] = 1 AND " +
                    "[MON_Activo] = 1");

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_MONEDA_DECIMALES",
                    "[MON_Decimales] >= 0 AND " +
                    "[MON_Decimales] <= 6");
            });
        });
    }
}