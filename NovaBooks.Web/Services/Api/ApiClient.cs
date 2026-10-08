using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using NovaBooks.Web.Services.Session;

namespace NovaBooks.Web.Services.Api;

/// <summary>
/// Cliente tipado de NovaBooks.Api. Agrega el token Bearer, traduce
/// ProblemDetails y errores de red a ApiResult, y ante un 401 cierra la
/// sesión y redirige al login. Nunca registra tokens ni contraseñas.
/// </summary>
public sealed class ApiClient
{
    public const string NetworkMessage =
        "No fue posible comunicarse con el servidor. " +
        "Verifique que NovaBooks.Api esté en ejecución.";

    public const string TimeoutMessage =
        "El servidor tardó demasiado en responder. Intente nuevamente.";

    public const string StartingMessage =
        "El sistema se está preparando. Intente nuevamente en unos segundos.";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly SessionState _session;
    private readonly NavigationManager _navigation;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(
        HttpClient httpClient,
        SessionState session,
        NavigationManager navigation,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _session = session;
        _navigation = navigation;
        _logger = logger;
    }

    public Task<ApiResult<T>> GetAsync<T>(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Get, uri, null, anonymous: false, cancellationToken);
    }

    public Task<ApiResult<T>> PostAsync<T>(
        string uri,
        object body,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Post, uri, body, anonymous: false, cancellationToken);
    }

    public Task<ApiResult<T>> PutAsync<T>(
        string uri,
        object body,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Put, uri, body, anonymous: false, cancellationToken);
    }

    public Task<ApiResult<T>> PatchAsync<T>(
        string uri,
        object body,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Patch, uri, body, anonymous: false, cancellationToken);
    }

    public Task<ApiResult<bool>> DeleteAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<bool>(HttpMethod.Delete, uri, null, anonymous: false, cancellationToken);
    }

    /// <summary>
    /// Envía un archivo como multipart/form-data (campo "file").
    /// </summary>
    public Task<ApiResult<T>> PostFileAsync<T>(
        string uri,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        StreamContent fileContent = new(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        MultipartFormDataContent form = new() { { fileContent, "file", fileName } };

        return SendAsync<T>(HttpMethod.Post, uri, form, anonymous: false, cancellationToken);
    }

    /// <summary>
    /// Envía una solicitud sin token y sin manejar el 401 como sesión
    /// expirada (usado por el login).
    /// </summary>
    public Task<ApiResult<T>> PostAnonymousAsync<T>(
        string uri,
        object body,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Post, uri, body, anonymous: true, cancellationToken);
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string uri,
        object? body,
        bool anonymous,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, uri);

        if (body is HttpContent content)
        {
            request.Content = content;
        }
        else if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        if (!anonymous && _session.AccessToken is { } token)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Tiempo de espera agotado en {Method} {Uri}.", method, uri);

            return ApiResult<T>.Fail(ApiFailure.Timeout, TimeoutMessage);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                "No fue posible conectar con la API en {Method} {Uri}: {Error}",
                method,
                uri,
                exception.Message);

            return ApiResult<T>.Fail(ApiFailure.Network, NetworkMessage);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return await ReadSuccessAsync<T>(response, cancellationToken);
            }

            ApiResult<T> failure =
                await ReadFailureAsync<T>(response, cancellationToken);

            if (failure.Failure == ApiFailure.Unauthorized && !anonymous)
            {
                await HandleExpiredSessionAsync();
            }

            return failure;
        }
    }

    private static async Task<ApiResult<T>> ReadSuccessAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        int status = (int)response.StatusCode;

        if (response.StatusCode == HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength == 0)
        {
            return ApiResult<T>.Success(
                typeof(T) == typeof(bool) ? (T)(object)true : default,
                status);
        }

        T? data = await response.Content.ReadFromJsonAsync<T>(
            JsonOptions,
            cancellationToken);

        return ApiResult<T>.Success(data, status);
    }

    private static async Task<ApiResult<T>> ReadFailureAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        int status = (int)response.StatusCode;
        string? detail = null;
        string? code = null;
        Dictionary<string, string[]> validationErrors = [];

        try
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(content))
            {
                using JsonDocument document = JsonDocument.Parse(content);
                JsonElement root = document.RootElement;

                detail = GetString(root, "detail");
                code = GetString(root, "code");

                if (root.TryGetProperty("errors", out JsonElement errors))
                {
                    ReadErrors(errors, validationErrors);
                }
            }
        }
        catch (JsonException)
        {
            // Respuesta sin ProblemDetails: se usa el mensaje por estado.
        }

        string? firstError = validationErrors.Values
            .SelectMany(messages => messages)
            .FirstOrDefault();

        (ApiFailure failure, string fallback) = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => (ApiFailure.Validation, "Revise los datos ingresados."),
            HttpStatusCode.Unauthorized => (ApiFailure.Unauthorized, "Su sesión expiró. Inicie sesión nuevamente."),
            HttpStatusCode.Forbidden => (ApiFailure.Forbidden, "No tiene permiso para realizar esta acción."),
            HttpStatusCode.NotFound => (ApiFailure.NotFound, "El recurso solicitado no existe."),
            HttpStatusCode.Conflict => (ApiFailure.Conflict, "La operación entra en conflicto con los datos existentes."),
            HttpStatusCode.ServiceUnavailable when code == "system_starting" =>
                (ApiFailure.SystemStarting, StartingMessage),
            HttpStatusCode.ServiceUnavailable => (ApiFailure.Unavailable, "El servicio no está disponible. Intente nuevamente en unos segundos."),
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => (ApiFailure.Timeout, TimeoutMessage),
            _ => (ApiFailure.Server, "Ocurrió un error inesperado en el servidor. Intente nuevamente.")
        };

        string message = failure switch
        {
            // 401 sin detalle propio (el login sí envía uno).
            ApiFailure.Unauthorized => detail ?? fallback,
            ApiFailure.Validation => detail ?? firstError ?? fallback,
            ApiFailure.Server => fallback,
            _ => detail ?? fallback
        };

        return ApiResult<T>.Fail(failure, message, status, code, validationErrors);
    }

    private static void ReadErrors(
        JsonElement errors,
        Dictionary<string, string[]> target)
    {
        if (errors.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in errors.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    target[property.Name] = property.Value
                        .EnumerateArray()
                        .Select(item => item.GetString() ?? string.Empty)
                        .ToArray();
                }
            }
        }
        else if (errors.ValueKind == JsonValueKind.Array)
        {
            target[string.Empty] = errors
                .EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();
        }
    }

    private static string? GetString(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out JsonElement value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private async Task HandleExpiredSessionAsync()
    {
        await _session.SignOutAsync();

        string returnUrl = _navigation.ToBaseRelativePath(_navigation.Uri);

        _navigation.NavigateTo(
            $"login?reason=expired&returnUrl={Uri.EscapeDataString("/" + returnUrl)}",
            replace: true);
    }
}
