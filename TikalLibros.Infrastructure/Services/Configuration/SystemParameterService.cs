using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Configuration;
using TikalLibros.Application.Interfaces;
using TikalLibros.Application.Validation;
using TikalLibros.Domain.Entities.Configuration;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Data.Seed;
using TikalLibros.Infrastructure.Services.Audit;

namespace TikalLibros.Infrastructure.Services.Configuration;

/// <summary>
/// Parámetros de la empresa principal. Cada cambio queda en la auditoría
/// con el valor anterior y el nuevo.
/// </summary>
public sealed class SystemParameterService : ISystemParameterService
{
    private readonly AppDbContext _context;
    private readonly AuditTrail _audit;

    public SystemParameterService(AppDbContext context, AuditTrail audit)
    {
        _context = context;
        _audit = audit;
    }

    private IQueryable<PB_PARAMETRO_SISTEMA> Parameters =>
        _context.ParametrosSistema.Where(parameter =>
            parameter.PAR_Activo && parameter.Empresa.EMP_Codigo == OrganizationSeeder.CompanyCode);

    public async Task<IReadOnlyCollection<SystemParameterResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await Parameters
            .AsNoTracking()
            .OrderBy(parameter => parameter.PAR_Clave)
            .Select(parameter => new SystemParameterResponse
            {
                Key = parameter.PAR_Clave,
                Value = parameter.PAR_Valor ?? string.Empty,
                DataType = parameter.PAR_TipoDato,
                Description = parameter.PAR_Descripcion,
                IsEditable = parameter.PAR_EsEditable,
                ModifiedAt = parameter.PAR_FechaModificacion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetDecimalAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        string? value = await Parameters
            .Where(parameter => parameter.PAR_Clave == key)
            .Select(parameter => parameter.PAR_Valor)
            .FirstOrDefaultAsync(cancellationToken);

        return TryParse(value, out decimal number)
            ? number
            : decimal.Parse(Definition(key).DefaultValue, CultureInfo.InvariantCulture);
    }

    public async Task<string> GetStringAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        string? value = await Parameters
            .Where(parameter => parameter.PAR_Clave == key)
            .Select(parameter => parameter.PAR_Valor)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(value) ? Definition(key).DefaultValue : value;
    }

    public async Task<OperationResult<SystemParameterResponse>> UpdateAsync(
        string key,
        UpdateSystemParameterRequest request,
        CancellationToken cancellationToken = default)
    {
        PB_PARAMETRO_SISTEMA? parameter = await Parameters
            .FirstOrDefaultAsync(item => item.PAR_Clave == key, cancellationToken);

        if (parameter is null || SystemParameterKeys.Defaults.All(item => item.Key != parameter.PAR_Clave))
        {
            return OperationResult<SystemParameterResponse>.Missing("El parámetro solicitado no existe.");
        }

        if (!parameter.PAR_EsEditable)
        {
            return OperationResult<SystemParameterResponse>.Conflict("Este parámetro no puede modificarse.");
        }

        string normalized;

        if (parameter.PAR_TipoDato == "STRING")
        {
            // Por ahora el único parámetro de texto es el NIT del emisor.
            normalized = ContactRules.NormalizeIdentification(request.Value);

            if (parameter.PAR_Clave == SystemParameterKeys.InvoiceIssuerNit && !ContactRules.IsValidNitFormat(normalized))
            {
                return OperationResult<SystemParameterResponse>.Failure("El NIT del emisor no es válido (dígitos y verificador final numérico o K).");
            }
        }
        else
        {
            if (!TryParse(request.Value, out decimal value))
            {
                return OperationResult<SystemParameterResponse>.Failure("El valor solo admite números.");
            }

            string? error = await ValidateAsync(parameter.PAR_Clave, value, cancellationToken);

            if (error is not null)
            {
                return OperationResult<SystemParameterResponse>.Failure(error);
            }

            normalized = value.ToString("0.##", CultureInfo.InvariantCulture);
        }
        string? before = parameter.PAR_Valor;

        parameter.PAR_Valor = normalized;
        parameter.PAR_FechaModificacion = DateTime.UtcNow;

        _audit.Add(
            AuditActions.Update,
            "ParametroSistema",
            parameter.PAR_Clave,
            $"Cambió el parámetro {parameter.PAR_Clave} de {before} a {normalized}.",
            [new AuditChange("Valor", before, normalized)]);

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<SystemParameterResponse>.Success(new SystemParameterResponse
        {
            Key = parameter.PAR_Clave,
            Value = normalized,
            DataType = parameter.PAR_TipoDato,
            Description = parameter.PAR_Descripcion,
            IsEditable = parameter.PAR_EsEditable,
            ModifiedAt = parameter.PAR_FechaModificacion
        });
    }

    private async Task<string?> ValidateAsync(string key, decimal value, CancellationToken cancellationToken)
    {
        switch (key)
        {
            case SystemParameterKeys.ReservationDays:
                return value != decimal.Truncate(value) || value is < 1 or > 30
                    ? "Los días de reserva deben ser un número entero entre 1 y 30."
                    : null;

            case SystemParameterKeys.ReturnDays:
                return value != decimal.Truncate(value) || value is < 1 or > 365
                    ? "Los días para devoluciones deben ser un número entero entre 1 y 365."
                    : null;

            case SystemParameterKeys.DiscountWithoutApproval:
                decimal maximum = await GetDecimalAsync(SystemParameterKeys.MaximumDiscount, cancellationToken);
                return value is < 0 or > 100
                    ? "El porcentaje debe estar entre 0 y 100."
                    : value > maximum
                        ? $"El descuento sin aprobación no puede ser mayor que el descuento máximo ({maximum:0.##} %)."
                        : null;

            case SystemParameterKeys.MaximumDiscount:
                decimal withoutApproval = await GetDecimalAsync(SystemParameterKeys.DiscountWithoutApproval, cancellationToken);
                return value is < 0 or > 100
                    ? "El porcentaje debe estar entre 0 y 100."
                    : value < withoutApproval
                        ? $"El descuento máximo no puede ser menor que el descuento sin aprobación ({withoutApproval:0.##} %)."
                        : null;

            default:
                return null;
        }
    }

    private static SystemParameterDefinition Definition(string key) =>
        SystemParameterKeys.Defaults.FirstOrDefault(item => item.Key == key)
        ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Parámetro desconocido.");

    private static bool TryParse(string? value, out decimal number) =>
        decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number);
}
