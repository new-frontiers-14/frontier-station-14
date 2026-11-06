using Content.Shared._NF.Research;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._NF.Research.UI;

/// <summary>
/// Utility class for creating research-themed UI elements
/// </summary>
public static class ResearchUIHelpers
{
    /// <summary>
    /// Create a StyleBoxFlat with research-appropriate colors
    /// </summary>
    /// <param name="backgroundColor">Background color</param>
    /// <param name="borderColor">Border color</param>
    /// <param name="borderThickness">Border thickness (default: 1)</param>
    /// <returns>Configured StyleBoxFlat</returns>
    public static StyleBoxFlat CreateResearchStyleBox(Color backgroundColor, Color borderColor,
        float borderThickness = 1f)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = backgroundColor,
            BorderColor = borderColor,
            BorderThickness = new Thickness(borderThickness),
            Padding = new Thickness(3),
            ContentMarginBottomOverride = 3,
            ContentMarginLeftOverride = 5,
            ContentMarginRightOverride = 5,
            ContentMarginTopOverride = 3,
        };
    }

    /// <summary>
    /// Create a research-themed StyleBoxFlat for a specific availability state
    /// </summary>
    /// <param name="availability">Research availability state</param>
    /// <param name="primaryColor">Primary discipline color</param>
    /// <param name="borderThickness">Border thickness (default: 2.5)</param>
    /// <returns>Configured StyleBoxFlat</returns>
    public static StyleBoxFlat CreateTechItemStyleBox(ResearchAvailability availability, Color primaryColor,
        float borderThickness = 2.5f)
    {
        var darkenFactor = ResearchColorScheme.GetBackgroundInterpolationFactor(availability);
        var backgroundColor = Color.InterpolateBetween(primaryColor, Color.Black, darkenFactor);
        var borderColor = ResearchColorScheme.GetTechBorderColor(availability);

        return CreateResearchStyleBox(backgroundColor, borderColor, borderThickness);
    }

    /// <summary>
    /// Create a research-themed RoundedStyleBoxFlat for a specific availability state
    /// </summary>
    /// <param name="availability">Research availability state</param>
    /// <param name="primaryColor">Primary discipline color</param>
    /// <param name="borderThickness">Border thickness (default: 2.5)</param>
    /// <param name="cornerRadius">Corner radius (default: 8)</param>
    /// <returns>Configured RoundedStyleBoxFlat</returns>
    public static RoundedStyleBoxFlat CreateRoundedTechItemStyleBox(ResearchAvailability availability,
        Color primaryColor, float borderThickness = 2.5f, float cornerRadius = 8f)
    {
        var darkenFactor = ResearchColorScheme.GetBackgroundInterpolationFactor(availability);
        var backgroundColor = Color.InterpolateBetween(primaryColor, Color.Black, darkenFactor);
        var borderColor = ResearchColorScheme.GetTechBorderColor(availability);

        return new RoundedStyleBoxFlat
        {
            BackgroundColor = backgroundColor,
            BorderColor = borderColor,
            BorderThickness = new Thickness(borderThickness),
            CornerRadius = cornerRadius
        };
    }

    /// <summary>
    /// Create scrollbar StyleBoxFlat with research theme
    /// </summary>
    /// <param name="state">Scrollbar state (normal, hovered, grabbed)</param>
    /// <returns>Configured StyleBoxFlat</returns>
    public static StyleBoxFlat CreateScrollbarStyleBox(string state = "normal")
    {
        var color = state.ToLower() switch
        {
            "hovered" => ResearchColorScheme.UIColors.Scrollbar.Hovered,
            "grabbed" => ResearchColorScheme.UIColors.Scrollbar.Grabbed,
            _ => ResearchColorScheme.UIColors.Scrollbar.Normal
        };

        return new StyleBoxFlat
        {
            BackgroundColor = color,
            ContentMarginLeftOverride = 10,
            ContentMarginTopOverride = 10
        };
    }
}
