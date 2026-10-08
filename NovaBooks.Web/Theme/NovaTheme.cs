using MudBlazor;

namespace NovaBooks.Web.Theme;

/// <summary>
/// Paleta NovaBooks: azul marino (menú y títulos), terracota para acciones,
/// fondo crema y tarjetas cálidas. Debe coincidir con las variables de app.css.
/// </summary>
public static class NovaTheme
{
    public const string SerifFont = "Georgia, 'Iowan Old Style', 'Palatino Linotype', 'Times New Roman', serif";

    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#C4562F",
            PrimaryDarken = "#A9461F",
            PrimaryLighten = "#D9744F",
            Secondary = "#1F3049",
            SecondaryDarken = "#162338",
            SecondaryLighten = "#2D4466",
            Tertiary = "#2F7A52",
            Info = "#2F5C9A",
            Success = "#2F7A52",
            Warning = "#B7791F",
            Error = "#B4372B",
            Background = "#F7F3EE",
            BackgroundGray = "#F1ECE5",
            Surface = "#FFFFFF",
            AppbarBackground = "#FBF8F4",
            AppbarText = "#1F2A3A",
            DrawerBackground = "#1F3049",
            DrawerText = "#E7E1D8",
            DrawerIcon = "#C9C0B3",
            TextPrimary = "#1F2A3A",
            TextSecondary = "#5F6673",
            ActionDefault = "#5F6673",
            LinesDefault = "#E8E1D7",
            LinesInputs = "#D6CEC2",
            TableLines = "#EEE8DF",
            TableHover = "#FBF6F0",
            Divider = "#E8E1D7"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif"],
                FontSize = "0.875rem"
            },
            // Escala única: títulos en serif; textos, campos, tablas y botones
            // en sans a 14 px (MudBlazor usa 16 px por defecto).
            H4 = new H4Typography { FontFamily = [SerifFont], FontSize = "1.6rem", FontWeight = "700", LineHeight = "1.25" },
            H5 = new H5Typography { FontFamily = [SerifFont], FontSize = "1.25rem", FontWeight = "700", LineHeight = "1.3" },
            H6 = new H6Typography { FontFamily = [SerifFont], FontSize = "1.05rem", FontWeight = "700", LineHeight = "1.35" },
            Subtitle1 = new Subtitle1Typography { FontSize = "0.95rem", FontWeight = "600", LineHeight = "1.45" },
            Subtitle2 = new Subtitle2Typography { FontSize = "0.85rem", FontWeight = "600", LineHeight = "1.45" },
            Body1 = new Body1Typography { FontSize = "0.875rem", LineHeight = "1.5" },
            Body2 = new Body2Typography { FontSize = "0.8125rem", LineHeight = "1.45" },
            Caption = new CaptionTypography { FontSize = "0.75rem", LineHeight = "1.4" },
            Button = new ButtonTypography { FontSize = "0.85rem", TextTransform = "none", FontWeight = "600" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "232px",
            AppbarHeight = "60px"
        }
    };
}
