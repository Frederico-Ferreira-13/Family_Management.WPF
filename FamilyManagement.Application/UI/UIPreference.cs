using System;
using FamilyManagement.Domain.Common;
using System.Text.RegularExpressions;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Application.UI;

public sealed record UIPreference
{
    private static readonly HashSet<string> SupportedThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Light", "Dark", "System"
    };

    public string Theme { get; }
    public string AccentColor { get; }

    private static readonly HashSet<string> Themes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Light", "Dark", "System"
    };

    private UIPreference(string theme, string accentColor)
    {
        Theme = theme;
        AccentColor = accentColor;
    }

    public static Result<UIPreference> Create(string? theme, string? color)
    {
        if (string.IsNullOrWhiteSpace(theme) || !Themes.Contains(theme))
            return Error.Validation("Tema inválido.");

        if (string.IsNullOrWhiteSpace(color) || !Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$"))
            return Error.Validation("Cor inválida.");

        return new UIPreference(theme, color.ToUpperInvariant());
    }

    public static UIPreference Default()
        => new("Light", "#0078D7");
}