using Godot;
using System;

[Tool]
public partial class DecoratedPanel : PanelContainer {
    // @formatter:off
    
    [ExportGroup("State")]
    private bool isHovered = false;
    [Export] public bool IsHovered { get => isHovered; set { isHovered = value; OnChange(); } }

    private bool isClicked = false;
    [Export] public bool IsClicked { get => isClicked; set { isClicked = value; OnChange(); } }

    private bool isActive = false;
    [Export] public bool IsActive { get => isActive; set { isActive = value; OnChange(); } }

    [ExportGroup("Background")]
    private Color normalBackgroundColor = new Color("1B3344");
    [Export] public Color NormalBackgroundColor { get => normalBackgroundColor; set { normalBackgroundColor = value; OnChange(); } }

    private Color hoveredBackgroundColor = new Color("1B3344");
    [Export] public Color HoveredBackgroundColor { get => hoveredBackgroundColor; set { hoveredBackgroundColor = value; OnChange(); } }

    private Color clickedBackgroundColor = new Color("1B3344");
    [Export] public Color ClickedBackgroundColor { get => clickedBackgroundColor; set { clickedBackgroundColor = value; OnChange(); } }

    private Color activeBackgroundColor = new Color("1B3344");
    [Export] public Color ActiveBackgroundColor { get => activeBackgroundColor; set { activeBackgroundColor = value; OnChange(); } }

    [ExportGroup("Border")]
    private float borderThickness = 2f;
    [Export] public float BorderThickness { get => borderThickness; set { borderThickness = value; OnChange(); } }

    private Color normalBorderColor = new Color("101E28");
    [Export] public Color NormalBorderColor { get => normalBorderColor; set { normalBorderColor = value; OnChange(); } }

    private Color hoveredBorderColor = new Color("4F6D82");
    [Export] public Color HoveredBorderColor { get => hoveredBorderColor; set { hoveredBorderColor = value; OnChange(); } }

    private Color clickedBorderColor = new Color("1B3344", 0.3f);
    [Export] public Color ClickedBorderColor { get => clickedBorderColor; set { clickedBorderColor = value; OnChange(); } }

    private Color activeBorderColor = new Color("896240");
    [Export] public Color ActiveBorderColor { get => activeBorderColor; set { activeBorderColor = value; OnChange(); } }

    [ExportSubgroup("Animation")]
    private Color animationBorderColor = new Color("267F00");
    [Export] public Color AnimationBorderColor { get => animationBorderColor; set { animationBorderColor = value; OnChange(); } }

    private float animationDuration = 2f;
    [Export] public float AnimationDuration { get => animationDuration; set { animationDuration = value; OnChange(); } }
    
    // @formatter:on

    private float animationStartTime = -9999f;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        OnChange();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    private void OnChange() {
        GD.Print("Change");
        Color bgColor = NormalBackgroundColor;
        Color borderColor = NormalBorderColor;

        if (isClicked) bgColor = ClickedBackgroundColor;
        else if (isActive) bgColor = ActiveBackgroundColor;
        else if (isHovered) bgColor = HoveredBackgroundColor;

        if (isClicked) borderColor = ClickedBorderColor;
        else if (isActive) borderColor = ActiveBorderColor;
        else if (isHovered) borderColor = HoveredBorderColor;

        ShaderMaterial material = (ShaderMaterial)Material;
        material.SetShaderParameter("border_thickness", BorderThickness);
        material.SetShaderParameter("background_color", bgColor);
        material.SetShaderParameter("border_color", borderColor);
    }

    public override void _Draw() {
        // this only draws the mask for the border and content, the shader takes care of the rest
        // this is necessary because shaders do not have access to local pixel positions, only UV (0/0 - 1/1) or absolute screen pixel positions (so no way of detecting how many pixel away we are from the edge of this canvas item)
        Rect2 rect = GetRect();
        float addon = BorderThickness / 2f; // strokes are defined by their center, so we need to move everything half a width inwards to avoid drawing out of bounds
        Vector2 offset = new Vector2(addon + BorderThickness * 2f, addon);
        DrawRect(new Rect2(rect.Position + offset, rect.Size - offset * 2), new Color(0f, 0f, 0f), false, BorderThickness);
        offset = new Vector2(offset.Y, offset.X);
        DrawRect(new Rect2(rect.Position + offset, rect.Size - offset * 2), new Color(0f, 0f, 0f), false, BorderThickness);
        offset = new Vector2(BorderThickness * 3, BorderThickness * 3);
        DrawRect(new Rect2(rect.Position + offset, rect.Size - offset * 2), new Color(1f, 1f, 1f));
    }
}