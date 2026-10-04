using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.People;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class PeopleConfiguration
{
    public static void ConfigurePeople(this ModelBuilder modelBuilder)
    {
        ConfigureTipoPersona(modelBuilder);
        ConfigurePersona(modelBuilder);
        ConfigurePersonaNatural(modelBuilder);
        ConfigurePersonaJuridica(modelBuilder);
        ConfigureTipoIdentificacion(modelBuilder);
        ConfigurePersonaIdentificacion(modelBuilder);
        ConfigureTipoTelefono(modelBuilder);
        ConfigurePersonaTelefono(modelBuilder);
        ConfigurePersonaCorreo(modelBuilder);
        ConfigureTipoDireccion(modelBuilder);
        ConfigureDireccion(modelBuilder);
        ConfigurePersonaDireccion(modelBuilder);
    }

    private static void ConfigureTipoPersona(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_TIPO_PERSONA>(builder =>
        {
            builder.ToTable("PB_TIPO_PERSONA");

            builder.HasKey(x => x.TPR_TipoPersona);

            builder.Property(x => x.TPR_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.TPR_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.TPR_Descripcion)
                .HasMaxLength(250);

            builder.Property(x => x.TPR_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.TPR_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.TPR_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigurePersona(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PERSONA>(builder =>
        {
            builder.ToTable("PB_PERSONA");

            builder.HasKey(x => x.PER_Persona);

            builder.Property(x => x.PER_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.PER_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasOne(x => x.TipoPersona)
                .WithMany(x => x.Personas)
                .HasForeignKey(x => x.PER_TipoPersonaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePersonaNatural(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PERSONA_NATURAL>(builder =>
        {
            builder.ToTable("PB_PERSONA_NATURAL");

            builder.HasKey(x => x.PNA_PersonaId);

            builder.Property(x => x.PNA_PersonaId)
                .ValueGeneratedNever();

            builder.Property(x => x.PNA_PrimerNombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.PNA_SegundoNombre)
                .HasMaxLength(100);

            builder.Property(x => x.PNA_TercerNombre)
                .HasMaxLength(100);

            builder.Property(x => x.PNA_PrimerApellido)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.PNA_SegundoApellido)
                .HasMaxLength(100);

            builder.Property(x => x.PNA_ApellidoCasada)
                .HasMaxLength(100);

            builder.Property(x => x.PNA_FechaNacimiento)
                .HasColumnType("date");

            builder.HasOne(x => x.Persona)
                .WithOne(x => x.PersonaNatural)
                .HasForeignKey<PB_PERSONA_NATURAL>(
                    x => x.PNA_PersonaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePersonaJuridica(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PERSONA_JURIDICA>(builder =>
        {
            builder.ToTable("PB_PERSONA_JURIDICA");

            builder.HasKey(x => x.PJU_PersonaId);

            builder.Property(x => x.PJU_PersonaId)
                .ValueGeneratedNever();

            builder.Property(x => x.PJU_RazonSocial)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.PJU_NombreComercial)
                .HasMaxLength(250);

            builder.Property(x => x.PJU_FechaConstitucion)
                .HasColumnType("date");

            builder.HasOne(x => x.Persona)
                .WithOne(x => x.PersonaJuridica)
                .HasForeignKey<PB_PERSONA_JURIDICA>(
                    x => x.PJU_PersonaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTipoIdentificacion(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_TIPO_IDENTIFICACION>(builder =>
        {
            builder.ToTable("PB_TIPO_IDENTIFICACION");

            builder.HasKey(x => x.TID_TipoIdentificacion);

            builder.Property(x => x.TID_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.TID_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.TID_LongitudMinima);

            builder.Property(x => x.TID_LongitudMaxima);

            builder.Property(x => x.TID_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.TID_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.TID_Nombre)
                .IsUnique();

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_TIPO_IDENTIFICACION_LONGITUD",
                    "[TID_LongitudMinima] IS NULL OR " +
                    "[TID_LongitudMaxima] IS NULL OR " +
                    "[TID_LongitudMaxima] >= [TID_LongitudMinima]");
            });
        });
    }

    private static void ConfigurePersonaIdentificacion(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PERSONA_IDENTIFICACION>(builder =>
        {
            builder.ToTable("PB_PERSONA_IDENTIFICACION");

            builder.HasKey(x => x.PID_PersonaIdentificacion);

            builder.Property(x => x.PID_Numero)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.PID_FechaEmision)
                .HasColumnType("date");

            builder.Property(x => x.PID_FechaVencimiento)
                .HasColumnType("date");

            builder.Property(x => x.PID_Principal)
                .HasDefaultValue(false);

            builder.Property(x => x.PID_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => new
            {
                x.PID_TipoIdentificacionId,
                x.PID_Numero
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.PID_PersonaId,
                x.PID_Principal
            })
            .IsUnique()
            .HasFilter(
                "[PID_Principal] = 1 AND [PID_Activo] = 1");

            builder.HasOne(x => x.Persona)
                .WithMany(x => x.Identificaciones)
                .HasForeignKey(x => x.PID_PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TipoIdentificacion)
                .WithMany(x => x.Identificaciones)
                .HasForeignKey(x => x.PID_TipoIdentificacionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_PERSONA_IDENTIFICACION_FECHAS",
                    "[PID_FechaVencimiento] IS NULL OR " +
                    "[PID_FechaEmision] IS NULL OR " +
                    "[PID_FechaVencimiento] >= [PID_FechaEmision]");
            });
        });
    }

    private static void ConfigureTipoTelefono(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_TIPO_TELEFONO>(builder =>
        {
            builder.ToTable("PB_TIPO_TELEFONO");

            builder.HasKey(x => x.TTE_TipoTelefono);

            builder.Property(x => x.TTE_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.TTE_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.TTE_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.TTE_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.TTE_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigurePersonaTelefono(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PERSONA_TELEFONO>(builder =>
        {
            builder.ToTable("PB_PERSONA_TELEFONO");

            builder.HasKey(x => x.PTE_PersonaTelefono);

            builder.Property(x => x.PTE_Numero)
                .HasMaxLength(25)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.PTE_Extension)
                .HasMaxLength(10)
                .IsUnicode(false);

            builder.Property(x => x.PTE_Principal)
                .HasDefaultValue(false);

            builder.Property(x => x.PTE_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => new
            {
                x.PTE_PersonaId,
                x.PTE_Numero
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.PTE_PersonaId,
                x.PTE_Principal
            })
            .IsUnique()
            .HasFilter(
                "[PTE_Principal] = 1 AND [PTE_Activo] = 1");

            builder.HasOne(x => x.Persona)
                .WithMany(x => x.Telefonos)
                .HasForeignKey(x => x.PTE_PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TipoTelefono)
                .WithMany(x => x.Telefonos)
                .HasForeignKey(x => x.PTE_TipoTelefonoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePersonaCorreo(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PERSONA_CORREO>(builder =>
        {
            builder.ToTable("PB_PERSONA_CORREO");

            builder.HasKey(x => x.PCO_PersonaCorreo);

            builder.Property(x => x.PCO_Correo)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.PCO_Principal)
                .HasDefaultValue(false);

            builder.Property(x => x.PCO_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => new
            {
                x.PCO_PersonaId,
                x.PCO_Correo
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.PCO_PersonaId,
                x.PCO_Principal
            })
            .IsUnique()
            .HasFilter(
                "[PCO_Principal] = 1 AND [PCO_Activo] = 1");

            builder.HasOne(x => x.Persona)
                .WithMany(x => x.Correos)
                .HasForeignKey(x => x.PCO_PersonaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTipoDireccion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_TIPO_DIRECCION>(builder =>
        {
            builder.ToTable("PB_TIPO_DIRECCION");

            builder.HasKey(x => x.TDI_TipoDireccion);

            builder.Property(x => x.TDI_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.TDI_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.TDI_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.TDI_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.TDI_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigureDireccion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_DIRECCION>(builder =>
        {
            builder.ToTable("PB_DIRECCION");

            builder.HasKey(x => x.DIR_Direccion);

            builder.Property(x => x.DIR_Zona)
                .HasMaxLength(20);

            builder.Property(x => x.DIR_Linea1)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.DIR_Linea2)
                .HasMaxLength(250);

            builder.Property(x => x.DIR_CodigoPostal)
                .HasMaxLength(20)
                .IsUnicode(false);

            builder.Property(x => x.DIR_Referencia)
                .HasMaxLength(500);

            builder.Property(x => x.DIR_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.DIR_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.DIR_MunicipioId);

            builder.HasOne(x => x.Municipio)
                .WithMany(x => x.Direcciones)
                .HasForeignKey(x => x.DIR_MunicipioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePersonaDireccion(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_PERSONA_DIRECCION>(builder =>
        {
            builder.ToTable("PB_PERSONA_DIRECCION");

            builder.HasKey(x => x.PDI_PersonaDireccion);

            builder.Property(x => x.PDI_Principal)
                .HasDefaultValue(false);

            builder.Property(x => x.PDI_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => new
            {
                x.PDI_PersonaId,
                x.PDI_DireccionId,
                x.PDI_TipoDireccionId
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.PDI_PersonaId,
                x.PDI_Principal
            })
            .IsUnique()
            .HasFilter(
                "[PDI_Principal] = 1 AND [PDI_Activo] = 1");

            builder.HasOne(x => x.Persona)
                .WithMany(x => x.Direcciones)
                .HasForeignKey(x => x.PDI_PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Direccion)
                .WithMany(x => x.PersonasDirecciones)
                .HasForeignKey(x => x.PDI_DireccionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TipoDireccion)
                .WithMany(x => x.PersonasDirecciones)
                .HasForeignKey(x => x.PDI_TipoDireccionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}