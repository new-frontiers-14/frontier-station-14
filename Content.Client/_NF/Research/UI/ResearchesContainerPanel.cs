using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Content.Shared._NF.Research;
using Content.Shared.Research.Prototypes;
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
            // Special handling for Spread line type - draw with anti-overlap logic
            else if (dependentItem.Prototype.PrerequisiteLineType == PrerequisiteLineType.Spread && prerequisiteItems.Count > 1)
            {
                var lineColor = GetRefinedConnectionColor(prerequisiteItems.First(), dependentItem);
                DrawSpreadConnections(handle, prerequisiteItems, dependentItem, lineColor);
            }
            else
            {
                // Regular individual connections for all other line types
                foreach (var prerequisiteItem in prerequisiteItems)
                {
                    // Calculate connection points - use side connections for Spread type, center for others
                    Vector2 startCoords, endCoords;

                    if (dependentItem.Prototype.PrerequisiteLineType == PrerequisiteLineType.Spread)
                    {
                        // For now, let's try using the same direction for both to see if that fixes the visual issue
                        startCoords = GetTechSideConnection(prerequisiteItem, dependentItem);  // Exit point from prerequisite
                        endCoords = GetTechSideConnection(dependentItem, prerequisiteItem);    // Entry point to dependent
                    }
                    else
                    {
                        startCoords = GetTechCenter(prerequisiteItem);
                        endCoords = GetTechCenter(dependentItem);
                    }

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
    /// Draw spread connections for multiple prerequisites with intelligent anti-overlap routing
    /// </summary>
    private void DrawSpreadConnections(DrawingHandleScreen handle, List<FancyResearchConsoleItem> prerequisites, FancyResearchConsoleItem dependent, Color color)
    {
        if (prerequisites.Count == 0)
            return;

        if (prerequisites.Count == 1)
        {
            // Single prerequisite - use regular spread connection with side connections
            var startCoords = GetTechSideConnection(prerequisites[0], dependent);
            var endCoords = GetTechSideConnection(dependent, prerequisites[0]);
            DrawSpreadConnection(handle, startCoords, endCoords, endCoords - startCoords, color);
            return;
        }

        // Multiple prerequisites - spread them out intelligently to avoid overlaps
        var prerequisiteConnections = prerequisites.Select(prereq => new
        {
            Item = prereq,
            StartCoord = GetTechSideConnection(prereq, dependent),      // Exit from prerequisite
            EndCoord = GetTechSideConnection(dependent, prereq)         // Entry to dependent
        }).ToList();

        // Sort prerequisites by angle relative to the dependent tech
        var sortedPrereqs = prerequisiteConnections
            .Select((conn, index) => new
            {
                StartCoord = conn.StartCoord,
                EndCoord = conn.EndCoord,
                Item = conn.Item,
                Angle = Math.Atan2(conn.StartCoord.Y - conn.EndCoord.Y, conn.StartCoord.X - conn.EndCoord.X)
            })
            .OrderBy(p => p.Angle)
            .ToList();

        // Create spread connections with increasing angular offsets
        for (int i = 0; i < sortedPrereqs.Count; i++)
        {
            var prereq = sortedPrereqs[i];
            var spreadIndex = i - (sortedPrereqs.Count - 1) / 2.0f; // Center the spread around 0

            DrawSpreadConnectionWithIndex(handle, prereq.StartCoord, prereq.EndCoord, color, spreadIndex, sortedPrereqs.Count);
        }
    }

    /// <summary>
    /// Draw an individual spread connection with angled routing at midpoint for anti-overlap
    /// </summary>
    private void DrawSpreadConnectionWithIndex(DrawingHandleScreen handle, Vector2 start, Vector2 end, Color color, float spreadIndex, int totalConnections)
    {
        const float baseOffset = 20f; // Base offset for separation
        const float indexMultiplier = 10f; // Additional offset per connection index

        var delta = end - start;

        // Create perpendicular offset direction for each connection to avoid overlaps
        var mainAngle = Math.Atan2(delta.Y, delta.X);
        var offsetAngle = mainAngle + Math.PI / 2;
        var offsetDirection = new Vector2((float)Math.Cos(offsetAngle), (float)Math.Sin(offsetAngle));

        // Calculate unique offset for this connection based on its index
        var totalOffset = baseOffset + (Math.Abs(spreadIndex) * indexMultiplier);
        var indexOffset = offsetDirection * (totalOffset * Math.Sign(spreadIndex));

        // Create angled routing with bend exactly at midpoint
        var midPoint = Vector2.Lerp(start, end, 0.5f) + indexOffset;

        // Draw two-segment angled line: start -> midpoint -> end
        DrawCleanLine(handle, start, midPoint, color);
        DrawCleanLine(handle, midPoint, end, color);
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
    /// Get connection point on the side-middle of a tech item facing toward another tech
    /// </summary>
    private Vector2 GetTechSideConnection(FancyResearchConsoleItem fromTech, FancyResearchConsoleItem toTech)
    {
        var fromRect = GetTechRect(fromTech);
        var toRect = GetTechRect(toTech);

        var fromCenter = fromRect.Center;
        var toCenter = toRect.Center;
        var direction = (toCenter - fromCenter).Normalized();

        // Calculate which side of the FROM tech box to exit from (toward the TO tech)
        var absX = Math.Abs(direction.X);
        var absY = Math.Abs(direction.Y);

        if (absX > absY)
        {
            // Horizontal-dominant connection - exit from the side facing the target
            if (direction.X > 0)
                return new Vector2(fromRect.Right, fromCenter.Y); // Exit right side to go right
            else
                return new Vector2(fromRect.Left, fromCenter.Y); // Exit left side to go left
        }
        else
        {
            // Vertical-dominant connection - exit from the side facing the target
            if (direction.Y > 0)
                return new Vector2(fromCenter.X, fromRect.Bottom); // Exit bottom side to go down
            else
                return new Vector2(fromCenter.X, fromRect.Top); // Exit top side to go up
        }
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

            case PrerequisiteLineType.Spread:
                DrawSpreadConnection(handle, start, end, delta, color);
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
    /// Draw spread connection that uses angled routing at midpoint to avoid tech boxes
    /// </summary>
    private void DrawSpreadConnection(DrawingHandleScreen handle, Vector2 start, Vector2 end, Vector2 delta, Color color)
    {
        const float straightLineThreshold = 15f;

        // For very aligned connections, just draw straight
        if (Math.Abs(delta.X) < straightLineThreshold || Math.Abs(delta.Y) < straightLineThreshold)
        {
            DrawCleanLine(handle, start, end, color);
            return;
        }

        // Create angled routing with the bend exactly at the midpoint
        const float avoidanceDistance = 30f; // Distance to offset the midpoint for collision avoidance

        // Calculate perpendicular direction for avoidance at midpoint
        var mainAngle = Math.Atan2(delta.Y, delta.X);
        var offsetAngle = mainAngle + Math.PI / 2;
        var avoidanceDirection = new Vector2((float)Math.Cos(offsetAngle), (float)Math.Sin(offsetAngle));

        // Determine avoidance direction based on connection direction for consistency
        // Use the direction of the connection to determine which side to offset
        // This ensures connections going in opposite directions offset to opposite sides
        var avoidanceMultiplier = delta.Y >= 0 ? 1f : -1f; // Consistent direction based on Y-direction
        var avoidanceOffset = avoidanceDirection * avoidanceDistance * avoidanceMultiplier;

        // Create angled path with bend exactly at midpoint: start -> midpoint+offset -> end
        var midPoint = Vector2.Lerp(start, end, 0.5f) + avoidanceOffset;

        // Draw two-segment angled line: start -> midpoint -> end
        DrawCleanLine(handle, start, midPoint, color);
        DrawCleanLine(handle, midPoint, end, color);
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
        return ResearchColorScheme.GetConnectionColor(dependent.Availability);
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
