using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using System.Linq;
using System.Numerics;

namespace Content.Client._NF.Research.UI;

/// <summary>
/// UI element for visualizing technologies prerequisites
/// </summary>
public sealed partial class ResearchesContainerPanel : LayoutContainer
{
    public ResearchesContainerPanel()
    {
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        // First draw all children (including parallax background and tech items)
        base.Draw(handle);

        // Then draw prerequisite lines ON TOP of everything for maximum visibility
        DrawPrerequisiteLines(handle);
    }

    private void DrawPrerequisiteLines(DrawingHandleScreen handle)
    {
        // Draw prerequisite lines with maximum visibility - bright colors and thick lines
        foreach (var child in Children)
        {
            if (child is not FancyResearchConsoleItem item)
                continue;

            if (item.Prototype.TechnologyPrerequisites.Count <= 0)
                continue;

            var prerequisiteItems = Children.Where(x => x is FancyResearchConsoleItem second &&
                item.Prototype.TechnologyPrerequisites.Contains(second.Prototype.ID));

            foreach (var prerequisiteItem in prerequisiteItems)
            {
                var startCoords = new Vector2(item.PixelPosition.X + item.PixelWidth / 2, item.PixelPosition.Y + item.PixelHeight / 2);
                var endCoords = new Vector2(prerequisiteItem.PixelPosition.X + prerequisiteItem.PixelWidth / 2, prerequisiteItem.PixelPosition.Y + prerequisiteItem.PixelHeight / 2);

                // Use bright magenta for absolute maximum visibility against any background
                var lineColor = Color.Magenta;

                // Draw very thick lines by using a larger offset grid
                for (int x = -3; x <= 3; x++)
                {
                    for (int y = -3; y <= 3; y++)
                    {
                        var offsetStartCoords = startCoords + new Vector2(x, y);
                        var offsetEndCoords = endCoords + new Vector2(x, y);

                        if (prerequisiteItem.PixelPosition.Y != item.PixelPosition.Y)
                        {
                            // Draw L-shaped connection for different Y positions
                            handle.DrawLine(offsetStartCoords, new(offsetEndCoords.X, offsetStartCoords.Y), lineColor);
                            handle.DrawLine(new(offsetEndCoords.X, offsetStartCoords.Y), offsetEndCoords, lineColor);
                        }
                        else
                        {
                            // Draw direct line for same Y position
                            handle.DrawLine(offsetStartCoords, offsetEndCoords, lineColor);
                        }
                    }
                }
            }
        }
    }
}
