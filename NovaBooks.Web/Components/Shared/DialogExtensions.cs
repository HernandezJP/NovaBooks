using MudBlazor;

namespace NovaBooks.Web.Components.Shared;

public static class DialogExtensions
{
    // Tamaños de modal en toda la aplicación:
    //  - Confirmaciones: ExtraSmall (444 px).
    //  - Formularios (una o dos columnas) y listas: Small (600 px).
    // En pantallas angostas el modal ocupa el ancho disponible.

    public static readonly DialogOptions FormOptions = new()
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        CloseButton = true,
        CloseOnEscapeKey = true,
        BackdropClick = false
    };

    /// <summary>Formularios de dos columnas; mismo ancho que FormOptions.</summary>
    public static readonly DialogOptions WideFormOptions = FormOptions;

    public static readonly DialogOptions ConfirmOptions = new()
    {
        MaxWidth = MaxWidth.ExtraSmall,
        FullWidth = true,
        CloseButton = false,
        CloseOnEscapeKey = true,
        BackdropClick = true
    };

    /// <summary>
    /// Muestra una confirmación y devuelve true si el usuario aceptó.
    /// </summary>
    public static async Task<bool> ConfirmAsync(
        this IDialogService dialogs,
        string title,
        string message,
        string confirmText,
        Color confirmColor = Color.Primary)
    {
        DialogParameters<ConfirmDialog> parameters = new()
        {
            { dialog => dialog.Title, title },
            { dialog => dialog.Message, message },
            { dialog => dialog.ConfirmText, confirmText },
            { dialog => dialog.ConfirmColor, confirmColor }
        };

        IDialogReference reference = await dialogs.ShowAsync<ConfirmDialog>(
            title,
            parameters,
            ConfirmOptions);

        DialogResult? result = await reference.Result;

        return result is { Canceled: false };
    }

    /// <summary>
    /// Abre un diálogo de formulario y devuelve el dato guardado, o default
    /// si se canceló.
    /// </summary>
    public static async Task<TResult?> ShowFormAsync<TDialog, TResult>(
        this IDialogService dialogs,
        string title,
        DialogParameters<TDialog> parameters,
        DialogOptions? options = null)
        where TDialog : Microsoft.AspNetCore.Components.IComponent
    {
        IDialogReference reference = await dialogs.ShowAsync<TDialog>(
            title,
            parameters,
            options ?? FormOptions);

        DialogResult? result = await reference.Result;

        return result is { Canceled: false, Data: TResult data } ? data : default;
    }
}
