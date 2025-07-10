using Robust.Shared.Maths;

namespace Content.Shared._NF.Research;

/// <summary>
/// Optimized configurable color scheme for research UI elements
/// </summary>
/// <remarks>
/// This class centralizes all research UI colors to eliminate hardcoded values and improve maintainability.
/// Colors are organized by functional categories and can be modified at runtime.
/// 
/// Example usage:
/// <code>
/// // Get tech item colors
/// var colors = ResearchColorScheme.GetTechItemColors(ResearchAvailability.Available);
/// 
/// // Get UI element colors
/// var scrollbarColors = ResearchColorScheme.UIColors.Scrollbar;
/// 
/// // Customize colors
/// ResearchColorScheme.SetTechItemColors(ResearchAvailability.Researched, 
///     background: Color.Green, border: Color.LightGreen);
/// </code>
/// </remarks>
public static class ResearchColorScheme
{
    /// <summary>
    /// Color configuration for technology item states
    /// </summary>
    public struct TechItemColors
    {
        public Color Background { get; set; }
        public Color Border { get; set; }
        public Color Hover { get; set; }
        public Color Selected { get; set; }
        public Color Connection { get; set; }
        public Color InfoText { get; set; }

        public TechItemColors(Color background, Color border, Color hover, Color selected, Color connection, Color? infoText = null)
        {
            Background = background;
            Border = border;
            Hover = hover;
            Selected = selected;
            Connection = connection;
            InfoText = infoText ?? border;
        }
    }

    /// <summary>
    /// UI element colors for scrollbars, panels, etc.
    /// </summary>
    public static class UIColors
    {
        /// <summary>
        /// Default tech item background color (dark blue-gray)
        /// </summary>
        public static Color DefaultTechBackground { get; set; } = Color.FromHex("#141F2F");

        /// <summary>
        /// Default tech item border color (medium blue)
        /// </summary>
        public static Color DefaultTechBorder { get; set; } = Color.FromHex("#4972A1");

        /// <summary>
        /// Default tech item hover color (medium blue)
        /// </summary>
        public static Color DefaultTechHover { get; set; } = Color.FromHex("#4972A1");

        /// <summary>
        /// Scrollbar colors
        /// </summary>
        public static class Scrollbar
        {
            public static Color Normal { get; set; } = Color.FromHex("#80808059");
            public static Color Hovered { get; set; } = Color.FromHex("#8C8C8C59");
            public static Color Grabbed { get; set; } = Color.FromHex("#8C8C8C59");
        }

        /// <summary>
        /// Interpolation factors for different availability states
        /// </summary>
        public static class InterpolationFactors
        {
            public static float Researched { get; set; } = 0.2f;
            public static float Available { get; set; } = 0.0f;
            public static float PrereqsMet { get; set; } = 0.0f;
            public static float Unavailable { get; set; } = 0.5f;
            public static float Default { get; set; } = 0.5f;
        }

        /// <summary>
        /// Color mixing factors for hover and selection states
        /// </summary>
        public static class MixingFactors
        {
            public static float Hover { get; set; } = 0.3f;
            public static float Selected { get; set; } = 0.5f;
        }
    }

    private static readonly Dictionary<ResearchAvailability, TechItemColors> TechItemColorCache = new();
    private static bool _cacheInvalidated = true;

    /// <summary>
    /// Technology item colors based on availability state
    /// </summary>
    private static readonly Dictionary<ResearchAvailability, TechItemColors> BaseTechItemColors = new()
    {
        [ResearchAvailability.Researched] = new TechItemColors(
            background: Color.LimeGreen,
            border: Color.LimeGreen,
            hover: Color.LimeGreen,
            selected: Color.LimeGreen,
            connection: Color.LimeGreen,
            infoText: Color.LimeGreen
        ),
        [ResearchAvailability.Available] = new TechItemColors(
            background: Color.FromHex("#e8fa25"),
            border: Color.FromHex("#e8fa25"),
            hover: Color.FromHex("#e8fa25"),
            selected: Color.FromHex("#e8fa25"),
            connection: Color.FromHex("#e8fa25"),
            infoText: Color.FromHex("#e8fa25")
        ),
        [ResearchAvailability.PrereqsMet] = new TechItemColors(
            background: Color.FromHex("#cca031"),
            border: Color.FromHex("#cca031"),
            hover: Color.FromHex("#cca031"),
            selected: Color.FromHex("#cca031"),
            connection: Color.FromHex("#cca031"),
            infoText: Color.Crimson
        ),
        [ResearchAvailability.Unavailable] = new TechItemColors(
            background: Color.Crimson,
            border: Color.Crimson,
            hover: Color.Crimson,
            selected: Color.Crimson,
            connection: Color.Crimson,
            infoText: Color.Crimson
        )
    };

    /// <summary>
    /// Get optimized tech item colors for a specific availability state
    /// </summary>
    /// <param name="availability">The research availability state</param>
    /// <returns>Complete color configuration for tech items</returns>
    public static TechItemColors GetTechItemColors(ResearchAvailability availability)
    {
        if (_cacheInvalidated)
        {
            RebuildCache();
        }

        return TechItemColorCache.TryGetValue(availability, out var colors)
            ? colors
            : TechItemColorCache[ResearchAvailability.Unavailable];
    }

    /// <summary>
    /// Get connection color for a specific research availability (optimized)
    /// </summary>
    /// <param name="availability">The research availability state</param>
    /// <returns>The appropriate color for connection lines</returns>
    public static Color GetConnectionColor(ResearchAvailability availability)
    {
        return GetTechItemColors(availability).Connection;
    }

    /// <summary>
    /// Get tech border color for a specific research availability (optimized)
    /// </summary>
    /// <param name="availability">The research availability state</param>
    /// <returns>The appropriate color for technology borders</returns>
    public static Color GetTechBorderColor(ResearchAvailability availability)
    {
        return GetTechItemColors(availability).Border;
    }

    /// <summary>
    /// Get info panel text color for a specific research availability (optimized)
    /// </summary>
    /// <param name="availability">The research availability state</param>
    /// <returns>The appropriate color for info panel text, or null for default</returns>
    public static Color? GetInfoPanelColor(ResearchAvailability availability)
    {
        var colors = GetTechItemColors(availability);
        return availability == ResearchAvailability.Available ? null : colors.InfoText;
    }

    /// <summary>
    /// Get interpolation factor for background color darkening based on availability
    /// </summary>
    /// <param name="availability">The research availability state</param>
    /// <returns>Factor to use for Color.InterpolateBetween with Color.Black</returns>
    public static float GetBackgroundInterpolationFactor(ResearchAvailability availability)
    {
        return availability switch
        {
            ResearchAvailability.Researched => UIColors.InterpolationFactors.Researched,
            ResearchAvailability.Available => UIColors.InterpolationFactors.Available,
            ResearchAvailability.PrereqsMet => UIColors.InterpolationFactors.PrereqsMet,
            ResearchAvailability.Unavailable => UIColors.InterpolationFactors.Unavailable,
            _ => UIColors.InterpolationFactors.Default
        };
    }

    /// <summary>
    /// Get hover color mixing factor
    /// </summary>
    public static float GetHoverMixingFactor() => UIColors.MixingFactors.Hover;

    /// <summary>
    /// Get selection color mixing factor
    /// </summary>
    public static float GetSelectionMixingFactor() => UIColors.MixingFactors.Selected;

    /// <summary>
    /// Set tech item colors for a specific availability state
    /// </summary>
    /// <param name="availability">The availability state to configure</param>
    /// <param name="background">Background color</param>
    /// <param name="border">Border color</param>
    /// <param name="hover">Hover color (optional, defaults to border)</param>
    /// <param name="selected">Selected color (optional, defaults to border)</param>
    /// <param name="connection">Connection line color (optional, defaults to border)</param>
    /// <param name="infoText">Info panel text color (optional, defaults to border)</param>
    public static void SetTechItemColors(ResearchAvailability availability, Color background, Color border,
        Color? hover = null, Color? selected = null, Color? connection = null, Color? infoText = null)
    {
        BaseTechItemColors[availability] = new TechItemColors(
            background: background,
            border: border,
            hover: hover ?? border,
            selected: selected ?? border,
            connection: connection ?? border,
            infoText: infoText ?? border
        );
        _cacheInvalidated = true;
    }

    /// <summary>
    /// Rebuild the performance cache
    /// </summary>
    private static void RebuildCache()
    {
        TechItemColorCache.Clear();
        foreach (var kvp in BaseTechItemColors)
        {
            TechItemColorCache[kvp.Key] = kvp.Value;
        }
        _cacheInvalidated = false;
    }

    /// <summary>
    /// Reset all colors to their default values
    /// </summary>
    public static void ResetToDefaults()
    {
        // Reset UI colors
        UIColors.DefaultTechBackground = Color.FromHex("#141F2F");
        UIColors.DefaultTechBorder = Color.FromHex("#4972A1");
        UIColors.DefaultTechHover = Color.FromHex("#4972A1");

        UIColors.Scrollbar.Normal = Color.FromHex("#80808059");
        UIColors.Scrollbar.Hovered = Color.FromHex("#8C8C8C59");
        UIColors.Scrollbar.Grabbed = Color.FromHex("#8C8C8C59");

        UIColors.InterpolationFactors.Researched = 0.2f;
        UIColors.InterpolationFactors.Available = 0.0f;
        UIColors.InterpolationFactors.PrereqsMet = 0.0f;
        UIColors.InterpolationFactors.Unavailable = 0.5f;
        UIColors.InterpolationFactors.Default = 0.5f;

        UIColors.MixingFactors.Hover = 0.3f;
        UIColors.MixingFactors.Selected = 0.5f;

        // Reset tech item colors
        BaseTechItemColors[ResearchAvailability.Researched] = new TechItemColors(
            background: Color.LimeGreen,
            border: Color.LimeGreen,
            hover: Color.LimeGreen,
            selected: Color.LimeGreen,
            connection: Color.LimeGreen,
            infoText: Color.LimeGreen
        );

        BaseTechItemColors[ResearchAvailability.Available] = new TechItemColors(
            background: Color.FromHex("#e8fa25"),
            border: Color.FromHex("#e8fa25"),
            hover: Color.FromHex("#e8fa25"),
            selected: Color.FromHex("#e8fa25"),
            connection: Color.FromHex("#e8fa25"),
            infoText: Color.FromHex("#e8fa25")
        );

        BaseTechItemColors[ResearchAvailability.PrereqsMet] = new TechItemColors(
            background: Color.FromHex("#cca031"),
            border: Color.FromHex("#cca031"),
            hover: Color.FromHex("#cca031"),
            selected: Color.FromHex("#cca031"),
            connection: Color.FromHex("#cca031"),
            infoText: Color.Crimson
        );

        BaseTechItemColors[ResearchAvailability.Unavailable] = new TechItemColors(
            background: Color.Crimson,
            border: Color.Crimson,
            hover: Color.Crimson,
            selected: Color.Crimson,
            connection: Color.Crimson,
            infoText: Color.Crimson
        );

        _cacheInvalidated = true;
    }

    /// <summary>
    /// Set unified colors for all states of a specific availability
    /// </summary>
    /// <param name="availability">The availability state to set colors for</param>
    /// <param name="color">The color to use for all elements</param>
    public static void SetUnifiedColor(ResearchAvailability availability, Color color)
    {
        SetTechItemColors(availability, color, color, color, color, color, color);
    }

    #region Legacy Compatibility Properties (Deprecated)

    /// <summary>
    /// Legacy compatibility - use GetTechItemColors instead
    /// </summary>
    [Obsolete("Use GetTechItemColors instead")]
    public static class ConnectionColors
    {
        public static Color Researched => GetTechItemColors(ResearchAvailability.Researched).Connection;
        public static Color Available => GetTechItemColors(ResearchAvailability.Available).Connection;
        public static Color PrereqsMet => GetTechItemColors(ResearchAvailability.PrereqsMet).Connection;
        public static Color Unavailable => GetTechItemColors(ResearchAvailability.Unavailable).Connection;
        public static Color Default => Color.FromHex("#808080");
    }

    /// <summary>
    /// Legacy compatibility - use GetTechItemColors instead
    /// </summary>
    [Obsolete("Use GetTechItemColors instead")]
    public static class TechBorderColors
    {
        public static Color Researched => GetTechItemColors(ResearchAvailability.Researched).Border;
        public static Color Available => GetTechItemColors(ResearchAvailability.Available).Border;
        public static Color PrereqsMet => GetTechItemColors(ResearchAvailability.PrereqsMet).Border;
        public static Color Unavailable => GetTechItemColors(ResearchAvailability.Unavailable).Border;
    }

    /// <summary>
    /// Legacy compatibility - use GetTechItemColors instead
    /// </summary>
    [Obsolete("Use GetTechItemColors instead")]
    public static class InfoPanelColors
    {
        public static Color Researched => GetTechItemColors(ResearchAvailability.Researched).InfoText;
        public static Color PrereqsMet => GetTechItemColors(ResearchAvailability.PrereqsMet).InfoText;
        public static Color Unavailable => GetTechItemColors(ResearchAvailability.Unavailable).InfoText;
    }

    /// <summary>
    /// Legacy compatibility - use UIColors.Scrollbar instead
    /// </summary>
    [Obsolete("Use UIColors.Scrollbar instead")]
    public static void SynchronizeConnectionAndBorderColors()
    {
        // No-op - colors are now automatically synchronized
    }

    #endregion
}
