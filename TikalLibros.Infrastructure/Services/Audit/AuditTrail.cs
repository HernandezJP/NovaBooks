using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TikalLibros.Domain.Entities.Audit;
using TikalLibros.Infrastructure.Data;

namespace TikalLibros.Infrastructure.Services.Audit;

/// <summary>Campo modificado: valor anterior y nuevo, ya formateados para leerse.</summary>
public sealed record AuditChange(string Field, string? Before, string? After)
{
    public static AuditChange Of(string field, object? before, object? after) =>
        new(field, Format(before), Format(after));

    internal static string? Format(object? value) => value switch
    {
        null => null,
        decimal number => number.ToString("0.####", CultureInfo.InvariantCulture),
        DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        bool flag => flag ? "Sí" : "No",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)
    };
}

/// <summary>Acciones que se registran en PB_AUDITORIA.</summary>
public static class AuditActions
{
    public const string Create = "CREAR";
    public const string Update = "MODIFICAR";
    public const string ChangeStatus = "CAMBIAR_ESTADO";
    public const string Delete = "ELIMINAR";
    public const string Issue = "EMITIR";
    public const string Confirm = "CONFIRMAR";
    public const string Approve = "APROBAR";
    public const string Reject = "RECHAZAR";
    public const string Cancel = "CANCELAR";
    public const string Annul = "ANULAR";
    public const string Return = "DEVOLVER";
    public const string Expire = "VENCER";
    public const string Sell = "VENDER";
    public const string Pay = "PAGAR";
    public const string Open = "ABRIR";
    public const string Close = "CERRAR";
}

/// <summary>
/// Registro de auditoría: quién (usuario del token), qué, cuándo y desde
/// dónde (IP, navegador, ruta). <see cref="Add"/> solo agrega el registro al
/// contexto para que se guarde en la misma transacción que la operación;
/// así no queda una operación sin auditoría ni una auditoría sin operación.
/// Nunca se registran contraseñas, tokens ni el cuerpo de las solicitudes.
/// </summary>
public sealed class AuditTrail
{
    /// <summary>Marca en HttpContext.Items: la operación ya tiene su auditoría detallada.</summary>
    public const string RecordedItemKey = "TikalLibros.AuditRecorded";

    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditTrail(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Add(
        string action,
        string entity,
        string? entityId,
        string description,
        IEnumerable<AuditChange>? changes = null)
    {
        HttpContext? http = _httpContextAccessor.HttpContext;

        PB_AUDITORIA entry = new()
        {
            AUD_UsuarioId = CurrentUserId(http),
            AUD_Accion = Truncate(action, 50)!,
            AUD_Entidad = Truncate(entity, 150)!,
            AUD_EntidadId = Truncate(entityId, 100),
            AUD_Descripcion = Truncate(description, 1000),
            AUD_DireccionIp = Truncate(http?.Connection.RemoteIpAddress?.ToString(), 45),
            AUD_UserAgent = Truncate(http?.Request.Headers.UserAgent.ToString(), 1000),
            AUD_Ruta = Truncate(http?.Request.Path.Value, 1000),
            AUD_MetodoHttp = Truncate(http?.Request.Method, 10),
            AUD_CorrelacionId = Truncate(http?.TraceIdentifier, 100),
            AUD_Fecha = DateTime.UtcNow
        };

        foreach (AuditChange change in changes ?? [])
        {
            if (change.Before == change.After)
            {
                continue;
            }

            entry.Detalles.Add(new PB_AUDITORIA_DETALLE
            {
                ADE_Campo = Truncate(change.Field, 150)!,
                ADE_ValorAnterior = change.Before,
                ADE_ValorNuevo = change.After
            });
        }

        _context.Auditorias.Add(entry);

        if (http is not null)
        {
            http.Items[RecordedItemKey] = true;
        }
    }

    /// <summary>Agrega y guarda de inmediato (operaciones sin transacción propia).</summary>
    public async Task RecordAsync(
        string action,
        string entity,
        string? entityId,
        string description,
        CancellationToken cancellationToken = default)
    {
        Add(action, entity, entityId, description);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Intento de inicio de sesión (PB_HISTORIAL_ACCESO), exitoso o no.</summary>
    public async Task RecordLoginAsync(
        int? userId,
        string userName,
        bool succeeded,
        string? failureReason,
        CancellationToken cancellationToken = default)
    {
        HttpContext? http = _httpContextAccessor.HttpContext;

        _context.HistorialAccesos.Add(new PB_HISTORIAL_ACCESO
        {
            HAC_UsuarioId = userId,
            HAC_NombreUsuario = Truncate(userName.Trim(), 256),
            HAC_Fecha = DateTime.UtcNow,
            HAC_Exitoso = succeeded,
            HAC_MotivoFallo = Truncate(failureReason, 500),
            HAC_DireccionIp = Truncate(http?.Connection.RemoteIpAddress?.ToString(), 45),
            HAC_UserAgent = Truncate(http?.Request.Headers.UserAgent.ToString(), 1000),
            HAC_CorrelacionId = Truncate(http?.TraceIdentifier, 100)
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static int? CurrentUserId(HttpContext? http) =>
        int.TryParse(http?.User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;

    private static string? Truncate(string? value, int length) =>
        string.IsNullOrEmpty(value) ? null : value.Length <= length ? value : value[..length];
}
