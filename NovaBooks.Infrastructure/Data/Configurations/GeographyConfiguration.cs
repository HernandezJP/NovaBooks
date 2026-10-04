using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Geography;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class GeographyConfiguration
{
    public static void ConfigureGeography(this ModelBuilder modelBuilder)
    {
        ConfigurePais(modelBuilder);
        ConfigureDepartamento(modelBuilder);
        ConfigureMunicipio(modelBuilder);
    }

    private static void ConfigurePais(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GEO_PAIS>(builder =>
        {
            builder.ToTable("GEO_PAIS");

            builder.HasKey(x => x.PAI_Pais);

            builder.Property(x => x.PAI_Codigo)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.PAI_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.PAI_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.PAI_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.PAI_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigureDepartamento(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GEO_DEPARTAMENTO>(builder =>
        {
            builder.ToTable("GEO_DEPARTAMENTO");

            builder.HasKey(x => x.DEP_Departamento);

            builder.Property(x => x.DEP_Codigo)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.DEP_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.DEP_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => new
            {
                x.DEP_PaisId,
                x.DEP_Codigo
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.DEP_PaisId,
                x.DEP_Nombre
            }).IsUnique();

            builder.HasOne(x => x.Pais)
                .WithMany(x => x.Departamentos)
                .HasForeignKey(x => x.DEP_PaisId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureMunicipio(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GEO_MUNICIPIO>(builder =>
        {
            builder.ToTable("GEO_MUNICIPIO");

            builder.HasKey(x => x.MUN_Municipio);

            builder.Property(x => x.MUN_Codigo)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.MUN_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.MUN_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => new
            {
                x.MUN_DepartamentoId,
                x.MUN_Codigo
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.MUN_DepartamentoId,
                x.MUN_Nombre
            }).IsUnique();

            builder.HasOne(x => x.Departamento)
                .WithMany(x => x.Municipios)
                .HasForeignKey(x => x.MUN_DepartamentoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}