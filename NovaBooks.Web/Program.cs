using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using NovaBooks.Web.Components;
using NovaBooks.Web.Services.Api;
using NovaBooks.Web.Services.Session;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(options =>
{
    options.SnackbarConfiguration.PositionClass =
        Defaults.Classes.Position.TopRight;
    options.SnackbarConfiguration.PreventDuplicates = true;
    options.SnackbarConfiguration.VisibleStateDuration = 4000;
});

builder.Services
    .AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection(ApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// La Web llama a NovaBooks.Api desde el servidor. La validación TLS se
// mantiene: en desarrollo use "dotnet dev-certs https --trust".
builder.Services.AddHttpClient<ApiClient>((services, client) =>
{
    ApiOptions options =
        services.GetRequiredService<IOptions<ApiOptions>>().Value;

    client.BaseAddress = new Uri(
        options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/");

    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MenuState>();
builder.Services.AddScoped<MenuApi>();
builder.Services.AddScoped<UsersApi>();
builder.Services.AddScoped<RolesApi>();
builder.Services.AddScoped<PermissionsApi>();
builder.Services.AddScoped<CustomersApi>();
builder.Services.AddScoped<ProductsApi>();
builder.Services.AddScoped<AuthorsApi>();
builder.Services.AddScoped<EditorialsApi>();
builder.Services.AddScoped<AuthenticationStateProvider, NovaAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
