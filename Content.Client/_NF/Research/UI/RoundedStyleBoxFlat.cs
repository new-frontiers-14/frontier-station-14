using System.Numerics;
using Robust.Client.Graphics;

namespace Content.Client._NF.Research.UI;

/// <summary>
/// A StyleBoxFlat with rounded corners support
/// </summary>
public sealed class RoundedStyleBoxFlat : StyleBox
{
    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }

    /// <summary>
    /// Thickness of the border, in virtual pixels.
    /// </summary>
    public Thickness BorderThickness { get; set; }

    /// <summary>
    /// Radius of the rounded corners, in virtual pixels.
    /// </summary>
    public float CornerRadius { get; set; } = 0f;

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var scaledRadius = CornerRadius * uiScale;
        var thickness = BorderThickness.Scale(uiScale);

        if (scaledRadius <= 0)
        {
            // Draw as regular rectangle if no corner radius
            DrawRegularRect(handle, box, thickness);
        }
        else
        {
            // Draw rounded rectangle
            DrawRoundedRect(handle, box, thickness, scaledRadius);
        }
    }

    private void DrawRegularRect(DrawingHandleScreen handle, UIBox2 box, Thickness thickness)
    {
        var (btl, btt, btr, btb) = thickness;

        // Draw borders
        if (btl > 0)
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + btl, box.Bottom), BorderColor);

        if (btt > 0)
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Right, box.Top + btt), BorderColor);

        if (btr > 0)
            handle.DrawRect(new UIBox2(box.Right - btr, box.Top, box.Right, box.Bottom), BorderColor);

        if (btb > 0)
            handle.DrawRect(new UIBox2(box.Left, box.Bottom - btb, box.Right, box.Bottom), BorderColor);

        // Draw background
        handle.DrawRect(thickness.Deflate(box), BackgroundColor);
    }

    private void DrawRoundedRect(DrawingHandleScreen handle, UIBox2 box, Thickness thickness, float radius)
    {
        // Clamp radius to not exceed half the box dimensions
        var maxRadius = Math.Min(box.Width, box.Height) / 2f;
        radius = Math.Min(radius, maxRadius);

        // Draw background first
        DrawRoundedBackground(handle, box, radius);

        // Draw border if needed
        if (thickness.Left > 0 || thickness.Top > 0 || thickness.Right > 0 || thickness.Bottom > 0)
        {
            DrawRoundedBorder(handle, box, thickness, radius);
        }
    }

    private List<Vector2> CreateRoundedRectVertices(UIBox2 box, float radius)
    {
        var vertices = new List<Vector2>();
        const int segments = 16;

        // Top-left
        var tl = new Vector2(box.Left + radius, box.Top + radius);
        for (var i = 0; i <= segments; i++)
        {
            var angle = MathF.PI + (MathF.PI / 2) * (i / (float)segments);
            vertices.Add(tl + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }

        // Top-right
        var tr = new Vector2(box.Right - radius, box.Top + radius);
        for (var i = 0; i <= segments; i++)
        {
            var angle = 3 * MathF.PI / 2 + (MathF.PI / 2) * (i / (float)segments);
            vertices.Add(tr + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }

        // Bottom-right
        var br = new Vector2(box.Right - radius, box.Bottom - radius);
        for (var i = 0; i <= segments; i++)
        {
            var angle = 0 + (MathF.PI / 2) * (i / (float)segments);
            vertices.Add(br + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }

        // Bottom-left
        var bl = new Vector2(box.Left + radius, box.Bottom - radius);
        for (var i = 0; i <= segments; i++)
        {
            var angle = MathF.PI / 2 + (MathF.PI / 2) * (i / (float)segments);
            vertices.Add(bl + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }

        return vertices;
    }

    private void DrawRoundedBackground(DrawingHandleScreen handle, UIBox2 box, float radius)
    {
        var vertices = CreateRoundedRectVertices(box, radius);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices.ToArray(), BackgroundColor);
    }

    private void DrawRoundedBorder(DrawingHandleScreen handle, UIBox2 box, Thickness thickness, float radius)
    {
        var outer = CreateRoundedRectVertices(box, radius);
        var innerBox = new UIBox2(box.Left + thickness.Left, box.Top + thickness.Top, box.Right - thickness.Right, box.Bottom - thickness.Bottom);
        var innerRadius = Math.Max(0, radius - Math.Max(thickness.Left, thickness.Top));
        var inner = CreateRoundedRectVertices(innerBox, innerRadius);

        var vertices = new List<Vector2>();
        for (var i = 0; i < outer.Count; i++)
        {
            vertices.Add(outer[i]);
            vertices.Add(inner[i]);
        }
        vertices.Add(outer[0]);
        vertices.Add(inner[0]);

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, vertices.ToArray(), BorderColor);
    }

    public RoundedStyleBoxFlat()
    {
    }

    public RoundedStyleBoxFlat(Color backgroundColor, float cornerRadius = 0f)
    {
        BackgroundColor = backgroundColor;
        CornerRadius = cornerRadius;
    }

    public RoundedStyleBoxFlat(RoundedStyleBoxFlat other) : base(other)
    {
        BackgroundColor = other.BackgroundColor;
        BorderColor = other.BorderColor;
        BorderThickness = other.BorderThickness;
        CornerRadius = other.CornerRadius;
    }

    protected override float GetDefaultContentMargin(Margin margin)
    {
        return margin switch
        {
            Margin.Top => BorderThickness.Top,
            Margin.Bottom => BorderThickness.Bottom,
            Margin.Right => BorderThickness.Right,
            Margin.Left => BorderThickness.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(margin), margin, null)
        };
    }
}

/// <summary>
/// A StyleBoxFlat with rounded corners that supports diagonal split colors for dual-discipline technologies
/// </summary>
public sealed class RoundedDualColorStyleBoxFlat : StyleBox
{
    public Color PrimaryColor { get; set; }
    public Color SecondaryColor { get; set; }
    public Color BorderColor { get; set; }

    /// <summary>
    /// Thickness of the border, in virtual pixels.
    /// </summary>
    public Thickness BorderThickness { get; set; }

    /// <summary>
    /// Radius of the rounded corners, in virtual pixels.
    /// </summary>
    public float CornerRadius { get; set; } = 0f;

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var scaledRadius = CornerRadius * uiScale;
        var thickness = BorderThickness.Scale(uiScale);

        if (scaledRadius <= 0)
        {
            // Draw as regular rectangle if no corner radius
            DrawRegularDualColorRect(handle, box, thickness);
        }
        else
        {
            // Draw rounded rectangle with dual colors
            DrawRoundedDualColorRect(handle, box, thickness, scaledRadius);
        }
    }

    private void DrawRegularDualColorRect(DrawingHandleScreen handle, UIBox2 box, Thickness thickness)
    {
        var contentBox = thickness.Deflate(box);

        // Draw diagonal split background
        DrawDiagonalSplit(handle, contentBox);

        // Draw borders
        var (btl, btt, btr, btb) = thickness;
        if (btl > 0)
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + btl, box.Bottom), BorderColor);

        if (btt > 0)
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Right, box.Top + btt), BorderColor);

        if (btr > 0)
            handle.DrawRect(new UIBox2(box.Right - btr, box.Top, box.Right, box.Bottom), BorderColor);

        if (btb > 0)
            handle.DrawRect(new UIBox2(box.Left, box.Bottom - btb, box.Right, box.Bottom), BorderColor);
    }

    private void DrawRoundedDualColorRect(DrawingHandleScreen handle, UIBox2 box, Thickness thickness, float radius)
    {
        // Clamp radius to not exceed half the box dimensions
        var maxRadius = Math.Min(box.Width, box.Height) / 2f;
        radius = Math.Min(radius, maxRadius);

        // Draw background first with diagonal split
        DrawRoundedDualColorBackground(handle, box, radius);

        // Draw border if needed
        if (thickness.Left > 0 || thickness.Top > 0 || thickness.Right > 0 || thickness.Bottom > 0)
        {
            DrawRoundedBorder(handle, box, thickness, radius);
        }
    }

    private void DrawDiagonalSplit(DrawingHandleScreen handle, UIBox2 box)
    {
        // Create diagonal split from top-left to bottom-right
        var topLeft = new Vector2(box.Left, box.Top);
        var topRight = new Vector2(box.Right, box.Top);
        var bottomLeft = new Vector2(box.Left, box.Bottom);
        var bottomRight = new Vector2(box.Right, box.Bottom);

        // Draw top-right triangle (primary color)
        var primaryTriangle = new[]
        {
            topLeft,
            topRight,
            bottomRight
        };
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, primaryTriangle, PrimaryColor);

        // Draw bottom-left triangle (secondary color)
        var secondaryTriangle = new[]
        {
            topLeft,
            bottomLeft,
            bottomRight
        };
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, secondaryTriangle, SecondaryColor);
    }

    private void DrawRoundedDualColorBackground(DrawingHandleScreen handle, UIBox2 box, float radius)
    {
        var vertices = CreateRoundedRectVertices(box, radius);
        var center = box.Center;

        // Create triangles for dual color rendering
        for (var i = 0; i < vertices.Count; i++)
        {
            var vertex1 = vertices[i];
            var vertex2 = vertices[(i + 1) % vertices.Count];

            // Determine which color to use based on position relative to the diagonal
            var color = IsAboveDiagonal(vertex1, box) ? PrimaryColor : SecondaryColor;

            // Draw triangle from center to edge
            var triangle = new[] { center, vertex1, vertex2 };
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, triangle, color);
        }
    }

    private bool IsAboveDiagonal(Vector2 point, UIBox2 box)
    {
        // Check if point is above the diagonal line from top-left to bottom-right
        var relativeX = (point.X - box.Left) / box.Width;
        var relativeY = (point.Y - box.Top) / box.Height;
        return relativeY < relativeX;
    }

    private List<Vector2> CreateRoundedRectVertices(UIBox2 box, float radius)
    {
        var vertices = new List<Vector2>();
        const int segments = 16;

        // Top-left
        var tl = new Vector2(box.Left + radius, box.Top + radius);
        for (var i = 0; i <= segments; i++)
        {
            var angle = MathF.PI + (MathF.PI / 2) * (i / (float)segments);
            vertices.Add(tl + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }

        // Top-right
        var tr = new Vector2(box.Right - radius, box.Top + radius);
        for (var i = 0; i <= segments; i++)
        {
            var angle = 3 * MathF.PI / 2 + (MathF.PI / 2) * (i / (float)segments);
            vertices.Add(tr + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }

        // Bottom-right
        var br = new Vector2(box.Right - radius, box.Bottom - radius);
        for (var i = 0; i <= segments; i++)
        {
            var angle = 0 + (MathF.PI / 2) * (i / (float)segments);
            vertices.Add(br + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }

        // Bottom-left
        var bl = new Vector2(box.Left + radius, box.Bottom - radius);
        for (var i = 0; i <= segments; i++)
        {
            var angle = MathF.PI / 2 + (MathF.PI / 2) * (i / (float)segments);
            vertices.Add(bl + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }

        return vertices;
    }

    private void DrawRoundedBorder(DrawingHandleScreen handle, UIBox2 box, Thickness thickness, float radius)
    {
        var outer = CreateRoundedRectVertices(box, radius);
        var innerBox = new UIBox2(box.Left + thickness.Left, box.Top + thickness.Top, box.Right - thickness.Right, box.Bottom - thickness.Bottom);
        var innerRadius = Math.Max(0, radius - Math.Max(thickness.Left, thickness.Top));
        var inner = CreateRoundedRectVertices(innerBox, innerRadius);

        var vertices = new List<Vector2>();
        for (var i = 0; i < outer.Count; i++)
        {
            vertices.Add(outer[i]);
            vertices.Add(inner[i]);
        }
        vertices.Add(outer[0]);
        vertices.Add(inner[0]);

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, vertices.ToArray(), BorderColor);
    }

    public RoundedDualColorStyleBoxFlat()
    {
    }

    public RoundedDualColorStyleBoxFlat(Color primaryColor, Color secondaryColor, float cornerRadius = 0f)
    {
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        CornerRadius = cornerRadius;
    }

    public RoundedDualColorStyleBoxFlat(RoundedDualColorStyleBoxFlat other) : base(other)
    {
        PrimaryColor = other.PrimaryColor;
        SecondaryColor = other.SecondaryColor;
        BorderColor = other.BorderColor;
        BorderThickness = other.BorderThickness;
        CornerRadius = other.CornerRadius;
    }

    protected override float GetDefaultContentMargin(Margin margin)
    {
        return margin switch
        {
            Margin.Top => BorderThickness.Top,
            Margin.Bottom => BorderThickness.Bottom,
            Margin.Right => BorderThickness.Right,
            Margin.Left => BorderThickness.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(margin), margin, null)
        };
    }
}
