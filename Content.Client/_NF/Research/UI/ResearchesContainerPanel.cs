using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Content.Shared._NF.Research;
using System.Linq;
using System.Numerics;
using System;

namespace Content.Client._NF.Research.UI;

/// <summary>
/// UI element for visualizing technologies prerequisites with simple L-shaped connections
/// </summary>
public sealed partial class ResearchesContainerPanel : LayoutContainer
{
    private float _animationTime = 0f;
    private const float AnimationSpeed = 1.5f;
    private const float PulseIntensity = 0.15f;
    
    public ResearchesContainerPanel()
    {
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        // Update animation timer
        _animationTime += (float)IoCManager.Resolve<IGameTiming>().FrameTime.TotalSeconds * AnimationSpeed;
        
        // First draw all children (tech items)
        base.Draw(handle);

        // Then draw simple prerequisite lines
        DrawSimplePrerequisiteLines(handle);
    }

    private void DrawSimplePrerequisiteLines(DrawingHandleScreen handle)
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

            foreach (var prerequisiteItem in prerequisiteItems)
            {
                // Calculate simple connection points
                var startCoords = GetSimpleConnectionPoint(prerequisiteItem, dependentItem, true);
                var endCoords = GetSimpleConnectionPoint(dependentItem, prerequisiteItem, false);

                // Determine line color based on dependent tech's availability
                var lineColor = GetRefinedConnectionColor(prerequisiteItem, dependentItem);
                
                // Add animation for available techs
                var animatedColor = lineColor;
                if (dependentItem.Availability == ResearchAvailability.Available)
                {
                    var pulseValue = (float)(Math.Sin(_animationTime) * PulseIntensity + 1.0);
                    animatedColor = Color.InterpolateBetween(lineColor, Color.White, pulseValue * 0.15f);
                }

                // Draw simple L-shaped connection
                DrawSimpleLShapeConnection(handle, startCoords, endCoords, animatedColor);
            }
        }
    }

    /// <summary>
    /// Get simple connection point on tech edge
    /// </summary>
    private Vector2 GetSimpleConnectionPoint(FancyResearchConsoleItem tech, FancyResearchConsoleItem otherTech, bool isStart)
    {
        var techRect = GetTechRect(tech);
        var otherRect = GetTechRect(otherTech);
        
        var techCenter = techRect.Center;
        var otherCenter = otherRect.Center;
        
        var deltaX = otherCenter.X - techCenter.X;
        var deltaY = otherCenter.Y - techCenter.Y;
        
        // Simple edge-based connections
        if (Math.Abs(deltaY) > Math.Abs(deltaX))
        {
            // Primarily vertical connection
            if (deltaY > 0) // Target is below
                return new Vector2(techCenter.X, techRect.Bottom);
            else // Target is above
                return new Vector2(techCenter.X, techRect.Top);
        }
        else
        {
            // Primarily horizontal connection
            if (deltaX > 0) // Target is to the right
                return new Vector2(techRect.Right, techCenter.Y);
            else // Target is to the left
                return new Vector2(techRect.Left, techCenter.Y);
        }
    }

    /// <summary>
    /// Draw a simple L-shaped connection between two points
    /// </summary>
    private void DrawSimpleLShapeConnection(DrawingHandleScreen handle, Vector2 start, Vector2 end, Color color)
    {
        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;
        
        // Check if it's a direct line (same row or column)
        if (Math.Abs(deltaX) < 10) // Same column - direct vertical
        {
            DrawCleanLine(handle, start, end, color);
            var arrowPos = Vector2.Lerp(start, end, 0.85f);
            DrawArrowHead(handle, arrowPos, (end - start).Normalized(), color);
        }
        else if (Math.Abs(deltaY) < 10) // Same row - direct horizontal
        {
            DrawCleanLine(handle, start, end, color);
            var arrowPos = Vector2.Lerp(start, end, 0.85f);
            DrawArrowHead(handle, arrowPos, (end - start).Normalized(), color);
        }
        else // Different row and column - L-shaped
        {
            // Create simple L-shape with a corner
            Vector2 corner;
            
            // Choose corner based on which direction is predominant
            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                // Horizontal-first L-shape
                corner = new Vector2(end.X, start.Y);
            }
            else
            {
                // Vertical-first L-shape
                corner = new Vector2(start.X, end.Y);
            }
            
            // Draw the L-shaped path
            DrawCleanLine(handle, start, corner, color);
            DrawCleanLine(handle, corner, end, color);
            
            // Draw arrow at the end
            var finalDirection = (end - corner).Normalized();
            if (finalDirection.LengthSquared() > 0.1f)
            {
                var arrowPos = end - finalDirection * 4f;
                DrawArrowHead(handle, arrowPos, finalDirection, color);
            }
        }
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
    /// Determine connection color based on availability (keeping the improved color system)
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

    /// <summary>
    /// Draw a directional arrow
    /// </summary>
    private void DrawArrowHead(DrawingHandleScreen handle, Vector2 position, Vector2 direction, Color color)
    {
        if (direction.LengthSquared() < 0.1f) return;
        
        var arrowSize = 5f;
        var arrowAngle = MathF.PI / 4; // 45 degrees
        
        var arrowLeft = position - Vector2.Transform(direction, Matrix3x2.CreateRotation(arrowAngle)) * arrowSize;
        var arrowRight = position - Vector2.Transform(direction, Matrix3x2.CreateRotation(-arrowAngle)) * arrowSize;
        
        handle.DrawLine(position, arrowLeft, color);
        handle.DrawLine(position, arrowRight, color);
    }
}
