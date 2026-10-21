using Microsoft.EntityFrameworkCore;
using TikalLibros.Domain.Entities.Configuration;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Data.Seed;

namespace TikalLibros.Infrastructure.Services.Documents;

/// <summary>Tipos de documento numerados con PB_SECUENCIA_DOCUMENTO.</summary>
public static class DocumentTypes
{
    public const string PurchaseOrder = "ORDEN_COMPRA";
    public const string PurchaseReception = "RECEPCION_COMPRA";
    public const string InventoryMovement = "MOVIMIENTO_INVENTARIO";
    public const string InventoryAdjustment = "AJUSTE_INVENTARIO";
    public const string CashMovement = "MOVIMIENTO_CAJA";

    public static string PrefixFor(string type) => type switch
    {
        PurchaseOrder => "OC-",
        PurchaseReception => "REC-",
        InventoryMovement => "MOV-",
        InventoryAdjustment => "AJU-",
        CashMovement => "MCJ-",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de documento desconocido.")
    };
}

/// <summary>
/// Entrega el siguiente número de un documento ("OC-000001"). Debe usarse
/// dentro de la transacción que guarda el documento: si la transacción se
/// revierte, el número no se consume. La columna de versión de la secuencia
/// evita que dos operaciones simultáneas obtengan el mismo número.
/// </summary>
public sealed class DocumentNumberService
{
    private const int NumberLength = 6;
    private const string DefaultSeries = "A";
    private const int MaxAttempts = 5;

    private readonly AppDbContext _context;

    public DocumentNumberService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> NextAsync(string documentType, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            PB_SECUENCIA_DOCUMENTO sequence = await GetOrCreateAsync(documentType, cancellationToken);

            sequence.SEC_UltimoNumero++;
            sequence.SEC_FechaModificacion = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);

                return $"{sequence.SEC_Prefijo}{sequence.SEC_UltimoNumero.ToString().PadLeft(sequence.SEC_LongitudNumero, '0')}";
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                // Otro proceso tomó el número: se recarga y se intenta de nuevo.
                await _context.Entry(sequence).ReloadAsync(cancellationToken);
            }
        }
    }

    private async Task<PB_SECUENCIA_DOCUMENTO> GetOrCreateAsync(string documentType, CancellationToken cancellationToken)
    {
        int companyId = await _context.Empresas
            .Where(company => company.EMP_Codigo == OrganizationSeeder.CompanyCode)
            .Select(company => company.EMP_Empresa)
            .SingleAsync(cancellationToken);

        PB_SECUENCIA_DOCUMENTO? sequence = await _context.SecuenciasDocumento
            .FirstOrDefaultAsync(
                item => item.SEC_EmpresaId == companyId &&
                        item.SEC_SucursalId == null &&
                        item.SEC_TipoDocumento == documentType &&
                        item.SEC_Serie == DefaultSeries,
                cancellationToken);

        if (sequence is not null)
        {
            return sequence;
        }

        sequence = new PB_SECUENCIA_DOCUMENTO
        {
            SEC_EmpresaId = companyId,
            SEC_TipoDocumento = documentType,
            SEC_Serie = DefaultSeries,
            SEC_Prefijo = DocumentTypes.PrefixFor(documentType),
            SEC_LongitudNumero = NumberLength,
            SEC_UltimoNumero = 0,
            SEC_Activo = true
        };

        _context.SecuenciasDocumento.Add(sequence);

        return sequence;
    }
}
