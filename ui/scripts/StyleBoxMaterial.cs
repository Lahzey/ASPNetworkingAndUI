using System;
using System.Collections.Generic;
using Godot;

[GlobalClass, Tool, Icon("res://ui/icons/StyleBoxMaterial.svg")]
public partial class StyleBoxMaterial : StyleBox {
    protected readonly PolygonRenderer defaultPolygonRenderer = new PolygonRenderer();

    // all of these static fields are hacks to get around the engines limitations
    private static bool framePreDrawSet = false;
    public static StyleBoxMaterial someRef = null; // this is a hack to have a reference to get the currently drawing canvas item (all style boxes are capable of that) from static context
    private static readonly Dictionary<Rid, List<StyleBoxMaterial>> CANVAS_STYLE_BOXES = new Dictionary<Rid, List<StyleBoxMaterial>>();

    private Material material;
    [Export] public Material Material { get => material; set { material = value; RecreateMaterialCopies(); EmitChanged(); } }

    private BackgroundModeOption backgroundMode;
    [Export] public BackgroundModeOption BackgroundMode { get => backgroundMode; set { backgroundMode = value; NotifyPropertyListChanged(); EmitChanged(); } }
    
    private Rect2 backgroundRelativeRect;
    [Export] public Rect2 BackgroundRelativeRect { get => backgroundRelativeRect; set { backgroundRelativeRect = value; EmitChanged(); } }
    
    private Color backgroundColor = Colors.White;
    [Export] public Color BackgroundColor { get => backgroundColor; set { backgroundColor = value; EmitChanged(); } }
    
    private Texture backgroundTexture;
    [Export] public Texture BackgroundTexture { get => backgroundTexture; set { backgroundTexture = value; EmitChanged(); } }

    private PolygonInfo backgroundPolygon;
    [Export] public PolygonInfo BackgroundPolygon {
        get => backgroundPolygon;
        set {
            if (backgroundPolygon != null) {
                backgroundPolygon.Changed -= EmitChanged;
                backgroundPolygon.Changed -= TryAdjustContentMargins;
            }
            backgroundPolygon = value;
            if (backgroundPolygon != null) {
                backgroundPolygon.Changed += EmitChanged;
                backgroundPolygon.Changed += TryAdjustContentMargins;
            }
            EmitChanged();
        }
    }

    private bool relativeContentMargins = true;
    [Export] public bool RelativeContentMargins { get => relativeContentMargins; set { relativeContentMargins = value; TryAdjustContentMargins(); NotifyPropertyListChanged(); EmitChanged(); } }
    
    private float additionalContentMarginLeft;
    [Export] public float AdditionalContentMarginLeft { get => additionalContentMarginLeft; set { additionalContentMarginLeft = value; TryAdjustContentMargins(); EmitChanged(); }}
    
    private float additionalContentMarginTop;
    [Export] public float AdditionalContentMarginTop { get => additionalContentMarginTop; set { additionalContentMarginTop = value; TryAdjustContentMargins(); EmitChanged(); }}
    
    private float additionalContentMarginRight;
    [Export] public float AdditionalContentMarginRight { get => additionalContentMarginRight; set { additionalContentMarginRight = value; TryAdjustContentMargins(); EmitChanged(); }}
    
    private float additionalContentMarginBottom;
    [Export] public float AdditionalContentMarginBottom { get => additionalContentMarginBottom; set { additionalContentMarginBottom = value; TryAdjustContentMargins(); EmitChanged(); }}

    private readonly Dictionary<Rid, Material> materialCopies = new Dictionary<Rid, Material>(); // each style box should have its own copy, so it can modify material params without affecting other style boxes using the same material
    private readonly Dictionary<Rid, bool> hasDrawn = new Dictionary<Rid, bool>();

    public enum BackgroundModeOption {
        RECTANGLE,
        TEXTURE,
        POLYGON
    }

    public override void _ValidateProperty(Godot.Collections.Dictionary property) {
        StringName stringName = property["name"].AsStringName();
        PropertyUsageFlags usage = property["usage"].As<PropertyUsageFlags>();
        switch (true) { // just using switch true with case guards as a prettier if
            case true when stringName == PropertyName.BackgroundRelativeRect && BackgroundMode != BackgroundModeOption.RECTANGLE:
            case true when stringName == PropertyName.BackgroundColor && BackgroundMode != BackgroundModeOption.RECTANGLE:
            case true when stringName == PropertyName.BackgroundTexture && BackgroundMode != BackgroundModeOption.TEXTURE:
            case true when stringName == PropertyName.BackgroundPolygon && BackgroundMode != BackgroundModeOption.POLYGON:
                property["usage"] = (int) PropertyUsageFlags.NoEditor;
                break;
            case true when stringName == StyleBox.PropertyName.ContentMarginBottom:
            case true when stringName == StyleBox.PropertyName.ContentMarginLeft:
            case true when stringName == StyleBox.PropertyName.ContentMarginRight:
            case true when stringName == StyleBox.PropertyName.ContentMarginTop:
                if (RelativeContentMargins) property["usage"] = (int) (usage  | PropertyUsageFlags.ReadOnly);
                break;
            case true when stringName.ToString().StartsWith("AdditionalContentMargin"):
                if (!RelativeContentMargins) property["usage"] = (int) PropertyUsageFlags.NoEditor;
                break;
        }
    }

    protected virtual void TryAdjustContentMargins() {
        if (!RelativeContentMargins) return;
        switch (BackgroundMode) {
            case BackgroundModeOption.RECTANGLE:
                Vector2 endOffset = -BackgroundRelativeRect.Size - BackgroundRelativeRect.Position;
                ContentMarginLeft = BackgroundRelativeRect.Position.X;
                ContentMarginTop = BackgroundRelativeRect.Position.Y;
                ContentMarginRight = endOffset.X;
                ContentMarginBottom = endOffset.Y;
                break;
            case BackgroundModeOption.TEXTURE:
                ContentMarginLeft = AdditionalContentMarginLeft;
                ContentMarginTop = AdditionalContentMarginTop;
                ContentMarginRight = AdditionalContentMarginRight;
                ContentMarginBottom = AdditionalContentMarginBottom;
                break;
            case BackgroundModeOption.POLYGON:
                float thickness = backgroundPolygon?.BorderThickness ?? 0;
                ContentMarginLeft = thickness + AdditionalContentMarginLeft;
                ContentMarginTop = thickness + AdditionalContentMarginTop;
                ContentMarginRight = thickness + AdditionalContentMarginRight;
                ContentMarginBottom = thickness + AdditionalContentMarginBottom;
                break;
        }
    }

    protected virtual PolygonRenderer GetPolygonRenderer() {
        return defaultPolygonRenderer;
    }

    protected virtual void DrawBackground(Rid toCanvasItem, Rect2 rect) {
        RenderingServerInstance renderingServer = RenderingServer.Singleton;
        switch (BackgroundMode) {
            case BackgroundModeOption.RECTANGLE:
                renderingServer.CanvasItemAddRect(toCanvasItem, new Rect2(rect.Position + BackgroundRelativeRect.Position, rect.Size + BackgroundRelativeRect.Size), BackgroundColor);
                break;
            case BackgroundModeOption.TEXTURE:
                if (IsInstanceValid(BackgroundTexture)) renderingServer.CanvasItemAddTextureRect(toCanvasItem, rect, BackgroundTexture.GetRid());
                break;
            case BackgroundModeOption.POLYGON:
                if (IsInstanceValid(BackgroundPolygon)) GetPolygonRenderer().Render(BackgroundPolygon, toCanvasItem, rect);
                break;
        }
    }

    protected virtual void SetMaterialParams(Material material) { }

    private static void EnsurePreDrawHooked() {
        if (framePreDrawSet) return;
        if (!RenderingServer.Singleton.IsConnected(RenderingServerInstance.SignalName.FramePreDraw, Callable.From(FramePreDraw))) // static context is reset by compiling, so FramePreDrawSet is not fully reliable
            RenderingServer.Singleton.FramePreDraw += FramePreDraw;
        framePreDrawSet = true;
    }

    private static void FramePreDraw() {
        // I do not know why this is not an engine feature to begin with, shader TIME does not reliably reflect the time available from outside
        RenderingServer.GlobalShaderParameterSet("ACTUAL_TIME", Time.GetTicksMsec() / 1000f);
    }

    private static void CanvasItemAfterDraw() {
        CanvasItem canvasItem = someRef.GetCurrentItemDrawn();
        List<StyleBoxMaterial> styleBoxes = CANVAS_STYLE_BOXES[canvasItem.GetCanvasItem()];
        for (int i = styleBoxes.Count - 1; i >= 0; i--) { // looping backwards allows removal while in the loop (by AfterLastDraw)
            StyleBoxMaterial styleBox = styleBoxes[i];
            if (!styleBox.hasDrawn.ContainsKey(canvasItem.GetCanvasItem()) || !styleBox.hasDrawn[canvasItem.GetCanvasItem()]) {
                styleBox.AfterLastDraw(canvasItem);
            } else {
                styleBox.hasDrawn[canvasItem.GetCanvasItem()] = false; // set to false so the draw loop must set it to true again to prove it's still drawing
            }
        }
    }

    private void OnFirstDraw(CanvasItem canvasItem) {
        Rid rid = canvasItem.GetCanvasItem();
        if (!CANVAS_STYLE_BOXES.ContainsKey(rid)) {
            CANVAS_STYLE_BOXES[rid] = new List<StyleBoxMaterial>();
            if (!canvasItem.IsConnected(CanvasItem.SignalName.Draw, Callable.From(CanvasItemAfterDraw))) // static context is reset by compiling, so CANVAS_STYLE_BOXES is not fully reliable
                canvasItem.Draw += CanvasItemAfterDraw;
        }
        CANVAS_STYLE_BOXES[rid].Add(this);
        materialCopies.Add(rid, (Material) Material.Duplicate());
    }

    // this method will not be called if the item is removed (and deleted) instead of just the style box changing, leaving a leftover value hasDrawn (should be fine though)
    private void AfterLastDraw(CanvasItem canvasItem) {
        Rid rid = canvasItem.GetCanvasItem();
        CANVAS_STYLE_BOXES[rid].Remove(this);
        if (CANVAS_STYLE_BOXES[rid].Count == 0) {
            canvasItem.Draw -= CanvasItemAfterDraw;
            CANVAS_STYLE_BOXES.Remove(rid);
            RenderingServer.Singleton.CanvasItemSetMaterial(rid, canvasItem.Material?.GetRid() ?? new Rid());
        }
        hasDrawn.Remove(rid);
        materialCopies[rid]?.Dispose();
        materialCopies.Remove(rid);
        RenderingServer.RequestFrameDrawnCallback(Callable.From(canvasItem.QueueRedraw)); // might not be necessary, but just to be sure (calling QueueRedraw directly will not work as we are technically still drawing)
    }

    // This currently just sets the material of the canvas item, meaning it removes any material that is already set on the canvas item (we do bring it back after StyleBox is removed)
    // Optimally we would render the mesh with a material set to it,
    // but that seems to not be supported, 2D mesh instances seem to simply ignore the mesh material and only use the canvas item material
    // Source: https://github.com/godotengine/godot/issues/51578 (I am pretty sure this has not yet been fixed)
    public override void _Draw(Rid toCanvasItem, Rect2 rect) {
        someRef = this;
        EnsurePreDrawHooked();
        if (IsInstanceValid(Material)) {
            CanvasItem canvasItem = GetCurrentItemDrawn();
            if (!hasDrawn.ContainsKey(toCanvasItem)) { // hasDrawn contains false for style boxes that have drawn last loop, but will contain nothing on the first draw
                OnFirstDraw(canvasItem);
                if (materialCopies[toCanvasItem] is ShaderMaterial shaderMaterial) {
                    float time = Time.GetTicksMsec() / 1000f;
                    shaderMaterial.SetShaderParameter("start_time", time);
                }
            }
            // we need to set the material each draw because other style boxes may have reset the material in their AfterLastDraw (that occurs AFTER the next style box has drawn the first time)
            RenderingServer.Singleton.CanvasItemSetMaterial(toCanvasItem, materialCopies[toCanvasItem].GetRid());
            SetMaterialParams(materialCopies[toCanvasItem]);
            hasDrawn[toCanvasItem] = true;
        }
        DrawBackground(toCanvasItem, rect);
    }

    private void RecreateMaterialCopies() {
        foreach (Rid rid in materialCopies.Keys) {
            materialCopies[rid]?.Dispose();
            materialCopies[rid] = (Material) (IsInstanceValid(Material) ? Material.Duplicate() : null);
        }
    }
}