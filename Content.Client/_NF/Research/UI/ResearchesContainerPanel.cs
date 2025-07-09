using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Content.Shared._NF.Research;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;

namespace Content.Client._NF.Research.UI;

/// <summary>
/// UI element for visualizing technologies prerequisites with configurable connection types
/// </summary>
public sealed partial class ResearchesContainerPanel : LayoutContainer
{
    public ResearchesContainerPanel()
    {

    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        // First draw all children (tech items)
        base.Draw(handle);

        // Then draw prerequisite lines
        DrawPrerequisiteLines(handle);
    }

    private void DrawPrerequisiteLines(DrawingHandleScreen handle)
    {
        foreach (var child in Children)
        {
            if (child is not FancyResearchConsoleItem dependentItem)
                continue;

            if (dependentItem.Prototype.TechnologyPrerequisites.Count <= 0)
                continue;

            var prerequisiteItems = Children.Where(x => x is FancyResearchConsoleItem second &&
                dependentItem.Prototype.TechnologyPrerequisites.Contains(second.Prototype.ID))
                .Cast<FancyResearchConsoleItem>().ToList();

            // Special handling for Tree line type - draw all prerequisites as a unified tree
            if (dependentItem.Prototype.PrerequisiteLineType == PrerequisiteLineType.Tree && prerequisiteItems.Count > 1)
            {
                var lineColor = GetRefinedConnectionColor(prerequisiteItems.First(), dependentItem);
                DrawTreeConnections(handle, prerequisiteItems, dependentItem, lineColor);
            }
            else
            {
                // Regular individual connections for all other line types
                foreach (var prerequisiteItem in prerequisiteItems)
                {
                    // Calculate connection points
                    var startCoords = GetTechCenter(prerequisiteItem);
                    var endCoords = GetTechCenter(dependentItem);

                    // Determine line color based on dependent tech's availability
                    var lineColor = GetRefinedConnectionColor(prerequisiteItem, dependentItem);

                    // Draw connection based on the dependent tech's line type configuration
                    DrawConfigurableConnection(handle, startCoords, endCoords, lineColor, dependentItem.Prototype.PrerequisiteLineType);
                }
            }
        }
    }

    /// <summary>
    /// Draw tree-style connections where multiple prerequisites branch into a single trunk
    /// </summary>
    private void DrawTreeConnections(DrawingHandleScreen handle, List<FancyResearchConsoleItem> prerequisites, FancyResearchConsoleItem dependent, Color color)
    {
        if (prerequisites.Count == 0)
            return;

        var endCoords = GetTechCenter(dependent);

        if (prerequisites.Count == 1)
        {
            // Single prerequisite - draw as simple connection
            var startCoords = GetTechCenter(prerequisites[0]);
            DrawTreeConnection(handle, startCoords, endCoords, endCoords - startCoords, color);
            return;
        }

        // Multiple prerequisites - create clean tree structure
        var prerequisiteCoords = prerequisites.Select(GetTechCenter).ToList();

        // Sort prerequisites by their position for consistent branching
        var sortedPrereqs = prerequisiteCoords
            .Select((coord, index) => new { Coord = coord, Item = prerequisites[index] })
            .OrderBy(p => p.Coord.Y)
            .ThenBy(p => p.Coord.X)
            .ToList();

        // Calculate a clean trunk position
        var avgX = sortedPrereqs.Average(p => p.Coord.X);
        var avgY = sortedPrereqs.Average(p => p.Coord.Y);
        var avgPos = new Vector2(avgX, avgY);

        // Position trunk junction at a reasonable distance from dependent
        var toDependent = (endCoords - avgPos).Normalized();
        var trunkDistance = Math.Min(80f, (endCoords - avgPos).Length() * 0.6f);
        var trunkPoint = endCoords - toDependent * trunkDistance;

        // Draw clean trunk line from junction to dependent
        DrawCleanLine(handle, trunkPoint, endCoords, color);

        // Draw clean branches from each prerequisite to the trunk junction
        foreach (var prereq in sortedPrereqs)
        {
            DrawCleanTreeBranch(handle, prereq.Coord, trunkPoint, color);
        }

        // Draw a small, clean junction indicator
        DrawCleanJunctionIndicator(handle, trunkPoint, color);
    }

    /// <summary>
    /// Draw a clean tree branch from prerequisite to trunk junction
    /// </summary>
    private void DrawCleanTreeBranch(DrawingHandleScreen handle, Vector2 start, Vector2 trunkPoint, Color color)
    {
        var delta = trunkPoint - start;
        var distance = delta.Length();

        // Avoid very short or overlapping connections
        if (distance < 10f)
            return;

        // Use a simple two-segment path for clean appearance
        Vector2 intermediatePoint;

        // Determine the best routing based on the spatial relationship
        if (Math.Abs(delta.X) > Math.Abs(delta.Y))
        {
            // Horizontal-dominant: go horizontal first, then vertical
            var horizontalDistance = delta.X * 0.7f; // Don't go all the way
            intermediatePoint = new Vector2(start.X + horizontalDistance, start.Y);
        }
        else
        {
            // Vertical-dominant: go vertical first, then horizontal  
            var verticalDistance = delta.Y * 0.7f; // Don't go all the way
            intermediatePoint = new Vector2(start.X, start.Y + verticalDistance);
        }

        // Draw the two-segment branch
        DrawCleanLine(handle, start, intermediatePoint, color);
        DrawCleanLine(handle, intermediatePoint, trunkPoint, color);
    }

    /// <summary>
    /// Draw a small, clean junction indicator at the trunk point
    /// </summary>
    private void DrawCleanJunctionIndicator(DrawingHandleScreen handle, Vector2 position, Color color)
    {
        const float size = 2.5f;

        // Draw a simple small square instead of complex shapes
        var rect = new UIBox2(
            position.X - size,
            position.Y - size,
            position.X + size,
            position.Y + size
        );

        handle.DrawRect(rect, color);
    }

    /// <summary>
    /// Draw tree-style connection for single prerequisite (fallback)
    /// </summary>
    private void DrawTreeConnection(DrawingHandleScreen handle, Vector2 start, Vector2 end, Vector2 delta, Color color)
    {
        // For single connections, tree style behaves like a clean L-shape
        const float straightLineThreshold = 15f;

        if (Math.Abs(delta.X) < straightLineThreshold || Math.Abs(delta.Y) < straightLineThreshold)
        {
            // Direct line for aligned connections
            DrawCleanLine(handle, start, end, color);
        }
        else
        {
            // Create a clean right-angle path
            Vector2 corner;

            // Always prefer the longer axis for the first segment
            if (Math.Abs(delta.X) > Math.Abs(delta.Y))
            {
                corner = new Vector2(end.X, start.Y);
            }
            else
            {
                corner = new Vector2(start.X, end.Y);
            }

            DrawCleanLine(handle, start, corner, color);
            DrawCleanLine(handle, corner, end, color);
        }
    }

    /// <summary>
    /// Get the center point of a tech item
    /// </summary>
    private Vector2 GetTechCenter(FancyResearchConsoleItem tech)
    {
        var techRect = GetTechRect(tech);
        return techRect.Center;
    }

    /// <summary>
    /// Draw connection based on the configured line type
    /// </summary>
    private void DrawConfigurableConnection(DrawingHandleScreen handle, Vector2 start, Vector2 end, Color color, PrerequisiteLineType lineType)
    {
        var delta = end - start;
        var distance = delta.Length();

        // Early exit for very short connections
        if (distance < 1f)
            return;

        switch (lineType)
        {
            case PrerequisiteLineType.LShape:
                DrawLShapeConnection(handle, start, end, delta, color);
                break;

            case PrerequisiteLineType.Diagonal:
                DrawDiagonalConnection(handle, start, end, color);
                break;

            case PrerequisiteLineType.Tree:
                DrawTreeConnection(handle, start, end, delta, color);
                break;

            default:
                // Fallback to L-shape
                DrawLShapeConnection(handle, start, end, delta, color);
                break;
        }
    }

    /// <summary>
    /// Draw L-shaped connection (default clean style)
    /// </summary>
    private void DrawLShapeConnection(DrawingHandleScreen handle, Vector2 start, Vector2 end, Vector2 delta, Color color)
    {
        const float straightLineThreshold = 15f;
        const float directDiagonalThreshold = 120f;

        // Check if it's a direct line (same row or column)
        if (Math.Abs(delta.X) < straightLineThreshold) // Same column - direct vertical
        {
            DrawCleanLine(handle, start, end, color);
        }
        else if (Math.Abs(delta.Y) < straightLineThreshold) // Same row - direct horizontal
        {
            DrawCleanLine(handle, start, end, color);
        }
        else if (delta.Length() < directDiagonalThreshold) // Close diagonal - use direct line
        {
            DrawCleanLine(handle, start, end, color);
        }
        else // Longer distance - use L-shape
        {
            DrawSimpleLShape(handle, start, end, delta, color);
        }
    }

    /// <summary>
    /// Draw simple L-shaped path
    /// </summary>
    private void DrawSimpleLShape(DrawingHandleScreen handle, Vector2 start, Vector2 end, Vector2 delta, Color color)
    {
        Vector2 corner;

        // Simple corner selection - prefer horizontal-first for better readability
        if (Math.Abs(delta.X) > Math.Abs(delta.Y))
        {
            corner = new Vector2(end.X, start.Y);
        }
        else
        {
            corner = new Vector2(start.X, end.Y);
        }

        DrawCleanLine(handle, start, corner, color);
        DrawCleanLine(handle, corner, end, color);
    }

    /// <summary>
    /// Draw direct diagonal connection
    /// </summary>
    private void DrawDiagonalConnection(DrawingHandleScreen handle, Vector2 start, Vector2 end, Color color)
    {
        DrawCleanLine(handle, start, end, color);
    }

    /// <summary>
    /// Get the visual rectangle of a tech item
    /// </summary>
    private UIBox2 GetTechRect(FancyResearchConsoleItem tech)
    {
        var position = new Vector2(tech.PixelPosition.X, tech.PixelPosition.Y);
        var size = new Vector2(tech.PixelWidth, tech.PixelHeight);

        var padding = 6f;
        return new UIBox2(
            position.X + padding,
            position.Y + padding,
            position.X + size.X - padding,
            position.Y + size.Y - padding
        );
    }

    /// <summary>
    /// Determine connection color based on availability
    /// </summary>
    private Color GetRefinedConnectionColor(FancyResearchConsoleItem prerequisite, FancyResearchConsoleItem dependent)
    {
        return dependent.Availability switch
        {
            // Researched - bright green
            ResearchAvailability.Researched => Color.FromHex("#32CD32"),

            // Available to research - bright yellow
            ResearchAvailability.Available => Color.FromHex("#FFD700"),

            // Prerequisites met but not available - orange
            ResearchAvailability.PrereqsMet => Color.FromHex("#FFA500"),

            // Unavailable - muted red
            ResearchAvailability.Unavailable => Color.FromHex("#CD5C5C"),

            // Default fallback - neutral gray
            _ => Color.FromHex("#808080")
        };
    }

    /// <summary>
    /// Draw a clean line with subtle thickness
    /// </summary>
    private void DrawCleanLine(DrawingHandleScreen handle, Vector2 start, Vector2 end, Color color)
    {
        handle.DrawLine(start, end, color);

        // Add subtle thickness
        var direction = (end - start).Normalized();
        var perpendicular = new Vector2(-direction.Y, direction.X) * 0.5f;

        var thicknessColor = new Color(color.R, color.G, color.B, color.A * 0.6f);
        handle.DrawLine(start + perpendicular, end + perpendicular, thicknessColor);
        handle.DrawLine(start - perpendicular, end - perpendicular, thicknessColor);
    }
}
