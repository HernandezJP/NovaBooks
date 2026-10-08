using System.ComponentModel.DataAnnotations;

namespace NovaBooks.Web.Services.Api;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>
    /// URL de NovaBooks.Api; también configurable con Api__BaseUrl.
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
