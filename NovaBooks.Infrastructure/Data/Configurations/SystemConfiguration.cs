using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Configuration;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class SystemConfiguration
{
    public static void ConfigureSystem(this ModelBuilder modelBuilder)
    {
        ConfigureEmpresa(modelBuilder);
        ConfigureParametroSistema(modelBuilder);
        ConfigureSecuenciaDocumento(modelBuilder);
    }

    private static void ConfigureEmpresa(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_EMPRESA>(builder =>
        {
            builder.ToTable("PB_EMPRESA");

            builder.HasKey(x => x.EMP_Empresa);

            builder.Property(x => x.EMP_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.EMP_NombreComercial)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.EMP_LogoRuta)
                .HasMaxLength(1000);

            builder.Property(x => x.EMP_SitioWeb)
                .HasMaxLength(500);

            builder.Property(x => x.EMP_Correo)
                .HasMaxLength(256);

            builder.Property(x => x.EMP_Telefono)
                .HasMaxLength(25)
                .IsUnicode(false);

            builder.Property(x => x.EMP_ZonaHoraria)
                .HasMaxLength(100)
                .HasDefaultValue("America/Guatemala")
                .IsRequired();

            builder.Property(x => x.EMP_FormatoFecha)
                .HasMaxLength(30)
                .HasDefaultValue("dd/MM/yyyy")
                .IsRequired();

            builder.Property(x => x.EMP_DecimalesCantidad)
                .HasDefaultValue(2);

            builder.Property(x => x.EMP_DecimalesPrecio)
                .HasDefaultValue(2);

            builder.Property(x => x.EMP_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.EMP_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.EMP_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.EMP_PersonaId)
                .IsUnique();

            builder.HasIndex(x => x.EMP_NombreComercial);

            builder.HasOne(x => x.Persona)
                .WithOne()
                .HasForeignKey<PB_EMPRESA>(
                    x => x.EMP_PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MonedaPredeterminada)
                .WithMany()
                .HasForeignKey(x => x.EMP_MonedaPredeterminadaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_EMPRESA_DECIMALES_CANTIDAD",
                    "[EMP_DecimalesCantidad] >= 0 AND " +
                    "[EMP_DecimalesCantidad] <= 6");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_EMPRESA_DECIMALES_PRECIO",
                    "[EMP_DecimalesPrecio] >= 0 AND " +
                    "[EMP_DecimalesPrecio] <= 6");
            });
        });
    }

    private static void ConfigureParametroSistema(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PARAMETRO_SISTEMA>(builder =>
        {
            builder.ToTable("PB_PARAMETRO_SISTEMA");

            builder.HasKey(x => x.PAR_ParametroSistema);

            builder.Property(x => x.PAR_Clave)
                .HasMaxLength(150)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.PAR_Valor)
                .HasMaxLength(4000);

            builder.Property(x => x.PAR_TipoDato)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("STRING")
                .IsRequired();

            builder.Property(x => x.PAR_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.PAR_EsEditable)
                .HasDefaultValue(true);

            builder.Property(x => x.PAR_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.PAR_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => new
            {
                x.PAR_EmpresaId,
                x.PAR_Clave
            }).IsUnique();

            builder.HasOne(x => x.Empresa)
                .WithMany(x => x.Parametros)
                .HasForeignKey(x => x.PAR_EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_PARAMETRO_SISTEMA_TIPO_DATO",
                    "[PAR_TipoDato] IN " +
                    "('STRING', 'INTEGER', 'DECIMAL', " +
                    "'BOOLEAN', 'DATE', 'JSON')");
            });
        });
    }

    private static void ConfigureSecuenciaDocumento(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_SECUENCIA_DOCUMENTO>(builder =>
        {
            builder.ToTable("PB_SECUENCIA_DOCUMENTO");

            builder.HasKey(x => x.SEC_SecuenciaDocumento);

            builder.Property(x => x.SEC_TipoDocumento)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.SEC_Serie)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.SEC_Prefijo)
                .HasMaxLength(30)
                .IsUnicode(false);

            builder.Property(x => x.SEC_Sufijo)
                .HasMaxLength(30)
                .IsUnicode(false);

            builder.Property(x => x.SEC_UltimoNumero)
                .HasDefaultValue(0L);

            builder.Property(x => x.SEC_LongitudNumero)
                .HasDefaultValue(8);

            builder.Property(x => x.SEC_ReiniciaAnualmente)
                .HasDefaultValue(false);

            builder.Property(x => x.SEC_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.SEC_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(x => x.SEC_Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            builder.HasIndex(x => new
            {
                x.SEC_EmpresaId,
                x.SEC_SucursalId,
                x.SEC_TipoDocumento,
                x.SEC_Serie
            }).IsUnique();

            builder.HasOne(x => x.Empresa)
                .WithMany(x => x.Secuencias)
                .HasForeignKey(x => x.SEC_EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Sucursal)
                .WithMany()
                .HasForeignKey(x => x.SEC_SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_SECUENCIA_ULTIMO_NUMERO",
                    "[SEC_UltimoNumero] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_SECUENCIA_LONGITUD",
                    "[SEC_LongitudNumero] >= 1 AND " +
                    "[SEC_LongitudNumero] <= 20");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_SECUENCIA_ULTIMO_ANIO",
                    "[SEC_UltimoAnio] IS NULL OR " +
                    "[SEC_UltimoAnio] BETWEEN 2000 AND 9999");
            });
        });
    }
}