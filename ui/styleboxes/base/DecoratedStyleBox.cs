using Godot;
using System.Collections.Generic;

[GlobalClass, Tool, Icon("res://ui/styleboxes/base/DecoratedStyleBox.svg")]
public partial class DecoratedStyleBox : StyleBoxMaterial {

    private readonly Dictionary<Rid, ArrayMesh> createdMeshes = new Dictionary<Rid, ArrayMesh>();
    
    private Color backgroundColor;
    [Export] public Color BackgroundColor { get => backgroundColor; set { backgroundColor = value; EmitChanged(); } }

    private Color borderColor;
    [Export] public Color BorderColor { get => borderColor; set { borderColor = value; EmitChanged(); } }

    private float borderThickness = 3;
    [Export] public float BorderThickness { get => borderThickness; set { borderThickness = value; TryAdjustContentMargins(); EmitChanged(); } }

    private bool autoAdjustContentMargins = true;
    [Export] public bool AutoAdjustContentMargins { get => autoAdjustContentMargins; set { autoAdjustContentMargins = value; TryAdjustContentMargins(); NotifyPropertyListChanged(); EmitChanged(); } }

    [ExportGroup("Animation")]
    private bool animated;
    [Export] public bool Animated { get => animated; set { animated = value; NotifyPropertyListChanged(); EmitChanged(); } }
    private float duration = 1f;
    [Export] public float Duration { get => duration; set { duration = value; EmitChanged(); } }
    private Color highlightColor;
    [Export] public Color HighlightColor { get => highlightColor; set { highlightColor = value; EmitChanged(); } }
    private bool loop = true;
    [Export] public bool Loop { get => loop; set { loop = value; EmitChanged(); } }
    private AnimationMode mode;
    [Export] public AnimationMode Mode { get => mode; set { mode = value; EmitChanged(); } }

    public enum AnimationMode : int {
        RADIAL_FILL = 0,
        RADIAL_TRACE = 1,
        BLINK = 2
    }

    public override void _ValidateProperty(Godot.Collections.Dictionary property) {
        base._ValidateProperty(property);
        StringName stringName = property["name"].AsStringName();
        if (!Animated && (stringName == PropertyName.Duration || stringName == PropertyName.HighlightColor || stringName == PropertyName.Loop || stringName == PropertyName.Mode)) {
            PropertyUsageFlags usage = property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly;
            property["usage"] = (int) usage;
        } else if (AutoAdjustContentMargins && (stringName == StyleBox.PropertyName.ContentMarginBottom || stringName == StyleBox.PropertyName.ContentMarginLeft || stringName == StyleBox.PropertyName.ContentMarginRight || stringName == StyleBox.PropertyName.ContentMarginTop)) {
            PropertyUsageFlags usage = property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly;
            property["usage"] = (int) usage;
        } else if (stringName == StyleBoxMaterial.PropertyName.Texture) {
            // Texture does nothing because we draw the background manually, so no need to show it to the user
            PropertyUsageFlags usage = PropertyUsageFlags.None;
            property["usage"] = (int) usage;
        }
    }

    private void TryAdjustContentMargins() {
        if (!AutoAdjustContentMargins) return;
        float totalThickness = BorderThickness * 3;
        ContentMarginBottom = totalThickness;
        ContentMarginLeft = totalThickness;
        ContentMarginRight = totalThickness;
        ContentMarginTop = totalThickness;
    }

    protected override void SetMaterialParams(Material material) {
        base.SetMaterialParams(material);
        if (material is not ShaderMaterial shaderMat) return;
        shaderMat.SetShaderParameter("background_color", BackgroundColor);
        shaderMat.SetShaderParameter("border_color", BorderColor);
        shaderMat.SetShaderParameter("animated", Animated);
        shaderMat.SetShaderParameter("animation_duration", Duration);
        shaderMat.SetShaderParameter("highlight_color", HighlightColor);
        shaderMat.SetShaderParameter("loop_animation", Loop);
        shaderMat.SetShaderParameter("animation_mode", (int) Mode);
    }

    protected override void DrawBackground(Rid toCanvasItem, Rect2 rect) {
        SurfaceTool surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        
        // we are using full green to mask the border and full blue to mask the content background (like a green screen), which means any actual content must not have exactly those colors (which should never really happen anyway)
        
        // add rect that expands top and bottom and sticks to content left and right
        Vector2 offset = new Vector2(BorderThickness * 2f, 0);
        AddRectOutlineToMesh(surfaceTool, new Rect2(rect.Position + offset, rect.Size - offset * 2), new Color(0, 1, 0), BorderThickness, rect);
        
        // add rect that expands left and right and sticks to content top and bottom
        offset = new Vector2(offset.Y, offset.X); // flip offset
        AddRectOutlineToMesh(surfaceTool, new Rect2(rect.Position + offset, rect.Size - offset * 2), new Color(0, 1, 0), BorderThickness, rect);
        
        // add content rect
        offset = new Vector2(BorderThickness * 3, BorderThickness * 3);
        AddRectToMesh(surfaceTool, new Rect2(rect.Position + offset, rect.Size - offset * 2), new Color(0, 0, 1), rect);

        // this may potentially cause memory issues when too many canvas items with this style box are destroyed, as we never do the cleanup
        ArrayMesh mesh;
        if (createdMeshes.TryGetValue(toCanvasItem, out mesh)) {
            mesh.Dispose();
            mesh = surfaceTool.Commit();
            createdMeshes[toCanvasItem] = mesh;
        } else {
            mesh = surfaceTool.Commit();
            createdMeshes.Add(toCanvasItem, mesh);
        }
        
        RenderingServer.Singleton.CanvasItemAddMesh(toCanvasItem, mesh.GetRid());
    }

    private void AddRectOutlineToMesh(SurfaceTool surfaceTool, Rect2 rect, Color color, float thickness, Rect2 totalRect) {
        // Top side
        AddRectToMesh(surfaceTool, new Rect2(rect.Position, new Vector2(rect.Size.X, thickness)), color, totalRect);
        // Bottom side
        AddRectToMesh(surfaceTool, new Rect2(new Vector2(rect.Position.X, rect.Position.Y + rect.Size.Y - thickness), new Vector2(rect.Size.X, thickness)), color, totalRect);
        // Left side
        AddRectToMesh(surfaceTool, new Rect2(rect.Position, new Vector2(thickness, rect.Size.Y)), color, totalRect);
        // Right side
        AddRectToMesh(surfaceTool, new Rect2(new Vector2(rect.Position.X + rect.Size.X - thickness, rect.Position.Y), new Vector2(thickness, rect.Size.Y)), color, totalRect);
    }

    private void AddRectToMesh(SurfaceTool surfaceTool, Rect2 rect, Color color, Rect2 totalRect) {
        Vector2 minUV = (rect.Position - totalRect.Position) / totalRect.Size;
        Vector2 maxUV = minUV + rect.Size / totalRect.Size;
        
        // Define the four vertices of the rectangle
        Vector3 topLeft = new Vector3(rect.Position.X, rect.Position.Y, 0);
        Vector3 topRight = new Vector3(rect.Position.X + rect.Size.X, rect.Position.Y, 0);
        Vector3 bottomLeft = new Vector3(rect.Position.X, rect.Position.Y + rect.Size.Y, 0);
        Vector3 bottomRight = new Vector3(rect.Position.X + rect.Size.X, rect.Position.Y + rect.Size.Y, 0);

        // Add vertices and color for each corner of the rectangle
        surfaceTool.SetColor(color);
        surfaceTool.SetUV(minUV);
        surfaceTool.AddVertex(topLeft);

        surfaceTool.SetColor(color);
        surfaceTool.SetUV(new Vector2(minUV.X, maxUV.Y));
        surfaceTool.AddVertex(bottomLeft);

        surfaceTool.SetColor(color);
        surfaceTool.SetUV(new Vector2(maxUV.X, maxUV.Y));
        surfaceTool.AddVertex(bottomRight);

        surfaceTool.SetColor(color);
        surfaceTool.SetUV(minUV);
        surfaceTool.AddVertex(topLeft);

        surfaceTool.SetColor(color);
        surfaceTool.SetUV(new Vector2(maxUV.X, maxUV.Y));
        surfaceTool.AddVertex(bottomRight);

        surfaceTool.SetColor(color);
        surfaceTool.SetUV(new Vector2(maxUV.X, minUV.Y));
        surfaceTool.AddVertex(topRight);
    }
}