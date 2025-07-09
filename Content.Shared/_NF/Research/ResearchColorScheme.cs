using Robust.Shared.Maths;

namespace Content.Shared._NF.Research;

/// <summary>
/// Configurable color scheme for research UI elements
/// </summary>
/// <remarks>
/// This class allows customization of all research UI colors. Colors can be modified at runtime
/// by setting the static properties in each nested class.
/// 
/// ConnectionColors are aligned with TechBorderColors by default to ensure visual consistency
/// between prerequisite lines and technology borders.
/// 
/// Example usage:
/// <code>
/// // Customize both connection and border colors together
/// ResearchColorScheme.SetUnifiedColor(ResearchAvailability.Researched, Color.Green);
/// 
/// // Or customize individually
/// ResearchColorScheme.ConnectionColors.Available = Color.Yellow;
/// ResearchColorScheme.TechBorderColors.Available = Color.Yellow;
/// 
/// // Keep them synchronized
/// ResearchColorScheme.SynchronizeConnectionAndBorderColors();
/// 
/// // Customize info panel text colors
/// ResearchColorScheme.InfoPanelColors.Researched = Color.LightGreen;
/// </code>
/// </remarks>
public static class ResearchColorScheme
{
    /// <summary>
    /// Colors for prerequisite connection lines based on availability
    /// </summary>
    public static class ConnectionColors
    {
        /// <summary>
        /// Researched technology connections - lime green (matches TechBorderColors.Researched)
        /// </summary>
        public static Color Researched { get; set; } = Color.LimeGreen;

        /// <summary>
        /// Available to research connections - bright yellow (matches TechBorderColors.Available)
        /// </summary>
        public static Color Available { get; set; } = Color.FromHex("#e8fa25");

        /// <summary>
        /// Prerequisites met but not available connections - golden orange (matches TechBorderColors.PrereqsMet)
        /// </summary>
        public static Color PrereqsMet { get; set; } = Color.FromHex("#cca031");

        /// <summary>
        /// Unavailable connections - crimson red (matches TechBorderColors.Unavailable)
        /// </summary>
        public static Color Unavailable { get; set; } = Color.Crimson;

        /// <summary>
        /// Default fallback connection color - neutral gray
        /// </summary>
        public static Color Default { get; set; } = Color.FromHex("#808080");
    }

    /// <summary>
    /// Colors for technology square borders based on availability
    /// </summary>
    public static class TechBorderColors
    {
        /// <summary>
        /// Researched technology border - lime green
        /// </summary>
        public static Color Researched { get; set; } = Color.LimeGreen;

        /// <summary>
        /// Available to research border - bright yellow
        /// </summary>
        public static Color Available { get; set; } = Color.FromHex("#e8fa25");

        /// <summary>
        /// Prerequisites met but not available border - golden orange
        /// </summary>
        public static Color PrereqsMet { get; set; } = Color.FromHex("#cca031");

        /// <summary>
        /// Unavailable technology border - crimson red
        /// </summary>
        public static Color Unavailable { get; set; } = Color.Crimson;
    }

    /// <summary>
    /// Colors for technology info panel text based on availability
    /// </summary>
    public static class InfoPanelColors
    {
        /// <summary>
        /// Researched technology text - lime green
        /// </summary>
        public static Color Researched { get; set; } = Color.LimeGreen;

        /// <summary>
        /// Prerequisites met but not available text - crimson red
        /// </summary>
        public static Color PrereqsMet { get; set; } = Color.Crimson;

        /// <summary>
        /// Unavailable technology text - crimson red
        /// </summary>
        public static Color Unavailable { get; set; } = Color.Crimson;
    }

    /// <summary>
    /// Get connection color for a specific research availability
    /// </summary>
    /// <param name="availability">The research availability state</param>
    /// <returns>The appropriate color for connection lines</returns>
    public static Color GetConnectionColor(ResearchAvailability availability)
    {
        return availability switch
        {
            ResearchAvailability.Researched => ConnectionColors.Researched,
            ResearchAvailability.Available => ConnectionColors.Available,
            ResearchAvailability.PrereqsMet => ConnectionColors.PrereqsMet,
            ResearchAvailability.Unavailable => ConnectionColors.Unavailable,
            _ => ConnectionColors.Default
        };
    }

    /// <summary>
    /// Get tech border color for a specific research availability
    /// </summary>
    /// <param name="availability">The research availability state</param>
    /// <returns>The appropriate color for technology borders</returns>
    public static Color GetTechBorderColor(ResearchAvailability availability)
    {
        return availability switch
        {
            ResearchAvailability.Researched => TechBorderColors.Researched,
            ResearchAvailability.Available => TechBorderColors.Available,
            ResearchAvailability.PrereqsMet => TechBorderColors.PrereqsMet,
            ResearchAvailability.Unavailable => TechBorderColors.Unavailable,
            _ => TechBorderColors.Unavailable
        };
    }

    /// <summary>
    /// Get info panel text color for a specific research availability
    /// </summary>
    /// <param name="availability">The research availability state</param>
    /// <returns>The appropriate color for info panel text, or null for default</returns>
    public static Color? GetInfoPanelColor(ResearchAvailability availability)
    {
        return availability switch
        {
            ResearchAvailability.Researched => InfoPanelColors.Researched,
            ResearchAvailability.PrereqsMet => InfoPanelColors.PrereqsMet,
            ResearchAvailability.Unavailable => InfoPanelColors.Unavailable,
            _ => null
        };
    }

    /// <summary>
    /// Reset all colors to their default values
    /// </summary>
    public static void ResetToDefaults()
    {
        // Connection colors (aligned with TechBorderColors)
        ConnectionColors.Researched = Color.LimeGreen;
        ConnectionColors.Available = Color.FromHex("#e8fa25");
        ConnectionColors.PrereqsMet = Color.FromHex("#cca031");
        ConnectionColors.Unavailable = Color.Crimson;
        ConnectionColors.Default = Color.FromHex("#808080");

        // Tech border colors
        TechBorderColors.Researched = Color.LimeGreen;
        TechBorderColors.Available = Color.FromHex("#e8fa25");
        TechBorderColors.PrereqsMet = Color.FromHex("#cca031");
        TechBorderColors.Unavailable = Color.Crimson;

        // Info panel colors
        InfoPanelColors.Researched = Color.LimeGreen;
        InfoPanelColors.PrereqsMet = Color.Crimson;
        InfoPanelColors.Unavailable = Color.Crimson;
    }

    /// <summary>
    /// Synchronize ConnectionColors with TechBorderColors to ensure visual consistency
    /// </summary>
    public static void SynchronizeConnectionAndBorderColors()
    {
        ConnectionColors.Researched = TechBorderColors.Researched;
        ConnectionColors.Available = TechBorderColors.Available;
        ConnectionColors.PrereqsMet = TechBorderColors.PrereqsMet;
        ConnectionColors.Unavailable = TechBorderColors.Unavailable;
    }

    /// <summary>
    /// Set both connection and border colors for a specific availability state
    /// </summary>
    /// <param name="availability">The availability state to set colors for</param>
    /// <param name="color">The color to use for both connections and borders</param>
    public static void SetUnifiedColor(ResearchAvailability availability, Color color)
    {
        switch (availability)
        {
            case ResearchAvailability.Researched:
                ConnectionColors.Researched = color;
                TechBorderColors.Researched = color;
                break;
            case ResearchAvailability.Available:
                ConnectionColors.Available = color;
                TechBorderColors.Available = color;
                break;
            case ResearchAvailability.PrereqsMet:
                ConnectionColors.PrereqsMet = color;
                TechBorderColors.PrereqsMet = color;
                break;
            case ResearchAvailability.Unavailable:
                ConnectionColors.Unavailable = color;
                TechBorderColors.Unavailable = color;
                break;
        }
    }
}
