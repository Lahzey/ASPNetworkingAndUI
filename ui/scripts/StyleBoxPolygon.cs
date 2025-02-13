using Godot;
using Godot.Collections;

[GlobalClass, Tool, Icon("res://ui/icons/StyleBoxDecorated.svg")]
public partial class StyleBoxPolygon : StyleBox {
    private readonly PolygonRenderer renderer = new PolygonRenderer();

    private PolygonInfo polygon;
    [Export] public PolygonInfo Polygon {
        get => polygon;
        set {
            if (polygon != null) polygon.Changed -= EmitChanged;
            polygon = value;
            if (polygon != null) polygon.Changed += EmitChanged;
            EmitChanged();
        }
    }

    public override void _Draw(Rid toCanvasItem, Rect2 rect) {
        if (polygon != null) renderer.Render(polygon, toCanvasItem, rect);
    }
}