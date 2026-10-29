using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using TikalLibros.Infrastructure.Services.Audit;

namespace TikalLibros.Api.Auditing;

/// <summary>
/// Audita toda operación que modifica datos y termina bien (POST, PUT,
/// PATCH o DELETE con respuesta 2xx): usuario, acción, entidad, id, ruta,
/// IP y navegador. Si el servicio ya registró una auditoría detallada (con
/// valores anteriores y nuevos) no se duplica. Nunca guarda el cuerpo de la
/// solicitud, así que contraseñas y tokens no llegan a la auditoría.
/// </summary>
public sealed class AuditActionFilter : IAsyncActionFilter
{
    // Último segmento de la ruta → acción auditada.
    private static readonly Dictionary<string, string> SegmentActions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["estado"] = AuditActions.ChangeStatus,
        ["emitir"] = AuditActions.Issue,
        ["confirmar"] = AuditActions.Confirm,
        ["aprobar"] = AuditActions.Approve,
        ["rechazar"] = AuditActions.Reject,
        ["cancelar"] = AuditActions.Cancel,
        ["anular"] = AuditActions.Annul,
        ["pagos"] = AuditActions.Pay,
        ["abrir"] = AuditActions.Open,
        ["cerrar"] = AuditActions.Close,
        ["permisos"] = "ASIGNAR_PERMISOS",
        ["roles"] = "ASIGNAR_ROLES",
        ["empleado"] = "VINCULAR_EMPLEADO",
        ["password"] = "RESTABLECER_CONTRASENA",
        ["imagen"] = "IMAGEN"
    };

    private readonly AuditTrail _audit;
    private readonly ILogger<AuditActionFilter> _logger;

    public AuditActionFilter(AuditTrail audit, ILogger<AuditActionFilter> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ActionExecutedContext executed = await next();

        HttpContext http = context.HttpContext;

        if (HttpMethods.IsGet(http.Request.Method) ||
            HttpMethods.IsHead(http.Request.Method) ||
            HttpMethods.IsOptions(http.Request.Method) ||
            executed.Exception is not null && !executed.ExceptionHandled ||
            http.Items.ContainsKey(AuditTrail.RecordedItemKey) ||
            context.ActionDescriptor is not ControllerActionDescriptor action ||
            HasAttribute<SkipAuditAttribute>(action) ||
            !IsSuccess(executed.Result))
        {
            return;
        }

        (string entity, string auditAction) = Describe(action, http.Request.Method);
        string? entityId = context.RouteData.Values.TryGetValue("id", out object? id)
            ? Convert.ToString(id)
            : ResultId(executed.Result);

        try
        {
            await _audit.RecordAsync(
                auditAction,
                entity,
                entityId,
                $"{auditAction} {entity}{(entityId is null ? string.Empty : $" #{entityId}")}",
                http.RequestAborted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // La operación ya se guardó; un fallo de auditoría no debe
            // convertirla en error para el usuario, pero sí quedar en el log.
            _logger.LogError(exception, "No fue posible registrar la auditoría de {Method} {Path}.",
                http.Request.Method, http.Request.Path);
        }
    }

    internal static (string Entity, string Action) Describe(ControllerActionDescriptor action, string method)
    {
        AuditEntityAttribute? actionAttribute = action.MethodInfo
            .GetCustomAttributes(typeof(AuditEntityAttribute), true)
            .OfType<AuditEntityAttribute>()
            .FirstOrDefault();

        AuditEntityAttribute? controllerAttribute = action.ControllerTypeInfo
            .GetCustomAttributes(typeof(AuditEntityAttribute), true)
            .OfType<AuditEntityAttribute>()
            .FirstOrDefault();

        string[] segments = (action.AttributeRouteInfo?.Template ?? string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        string entity = actionAttribute?.Entity
            ?? controllerAttribute?.Entity
            ?? string.Join('/', segments.Where(segment => !segment.StartsWith('{') && segment != "api"));

        string defaultAction =
            HttpMethods.IsPost(method) ? AuditActions.Create :
            HttpMethods.IsPut(method) ? AuditActions.Update :
            HttpMethods.IsPatch(method) ? AuditActions.ChangeStatus :
            AuditActions.Delete;

        string? last = segments.Length > 1 && segments[^2].StartsWith('{') ? segments[^1] : null;

        string auditAction = actionAttribute?.Action
            ?? (last is not null && SegmentActions.TryGetValue(last, out string? mapped)
                ? mapped == "IMAGEN" ? (HttpMethods.IsDelete(method) ? "ELIMINAR_IMAGEN" : "SUBIR_IMAGEN") : mapped
                : defaultAction);

        return (entity, auditAction);
    }

    private static bool HasAttribute<T>(ControllerActionDescriptor action) where T : Attribute =>
        action.MethodInfo.IsDefined(typeof(T), true) || action.ControllerTypeInfo.IsDefined(typeof(T), true);

    private static bool IsSuccess(IActionResult? result) => result switch
    {
        IStatusCodeActionResult { StatusCode: int status } => status is >= 200 and < 300,
        ObjectResult or EmptyResult or OkResult or NoContentResult => true,
        _ => false
    };

    // Para un alta, el id viene en la respuesta (propiedad Id).
    private static string? ResultId(IActionResult? result) =>
        result is ObjectResult { Value: { } value } &&
        value.GetType().GetProperty("Id") is { } property
            ? Convert.ToString(property.GetValue(value))
            : null;
}
