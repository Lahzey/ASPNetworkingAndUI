using System;
using System.Collections.Generic;
using Godot;

[GlobalClass, Tool, Icon("res://ui/styleboxes/base/StyleBoxMaterial.svg")]
public partial class StyleBoxMaterial : StyleBox {

    // all of these static fields are hacks to get around the engines limitations
    private static readonly List<RenderingServerInstance> HOOKED_PREDRAW_SERVERS = new List<RenderingServerInstance>();
    private static StyleBoxMaterial SOME_REF = null; // this is a hack to have a reference to get the currently drawing canvas item (all style boxes are capable of that) from static context
    private static readonly Dictionary<Rid, List<StyleBoxMaterial>> CANVAS_STYLE_BOXES = new Dictionary<Rid, List<StyleBoxMaterial>>();

    private Material material;
    [Export] public Material Material { get => material; set { material = value; RecreateMaterialCopies(); EmitChanged(); } }

    private Texture texture;
    [Export] public Texture Texture { get => texture; set { texture = value; EmitChanged(); } }

    private readonly Dictionary<Rid, Material> materialCopies = new Dictionary<Rid, Material>(); // each style box should have its own copy, so it can modify material params without affecting other style boxes using the same material
    private readonly Dictionary<Rid, bool> hasDrawn = new Dictionary<Rid, bool>();

    protected virtual void DrawBackground(Rid toCanvasItem, Rect2 rect) {
        RenderingServerInstance renderingServer = RenderingServer.Singleton;
        if (IsInstanceValid(Texture)) renderingServer.CanvasItemAddTextureRect(toCanvasItem, rect, Texture.GetRid());
        else renderingServer.CanvasItemAddRect(toCanvasItem, rect, Colors.White); // adding a texture is optional, defaulting to sending a full white rect to the material
    }

    protected virtual void SetMaterialParams(Material material) { }

    private static void EnsurePredrawHooked() {
        RenderingServerInstance renderingServer = RenderingServer.Singleton;
        if (!HOOKED_PREDRAW_SERVERS.Contains(renderingServer)) {
            HOOKED_PREDRAW_SERVERS.Add(renderingServer);
            renderingServer.FramePreDraw += Predraw;
        }
    }

    private static void Predraw() {
        // I do not know why this is not an engine feature to begin with, shader TIME does not reliably reflect the time available from outside
        RenderingServer.GlobalShaderParameterSet("ACTUAL_TIME", Time.GetTicksMsec() / 1000f);
    }

    private static void AfterDraw() {
        CanvasItem canvasItem = SOME_REF.GetCurrentItemDrawn();
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
            canvasItem.Draw += AfterDraw;
        }
        CANVAS_STYLE_BOXES[rid].Add(this);
        materialCopies.Add(rid, (Material) Material.Duplicate());
    }

    // this method will not be called if the item is removed (and deleted) instead of just the style box changing, leaving a leftover value hasDrawn (should be fine though)
    private void AfterLastDraw(CanvasItem canvasItem) {
        Rid rid = canvasItem.GetCanvasItem();
        CANVAS_STYLE_BOXES[rid].Remove(this);
        if (CANVAS_STYLE_BOXES[rid].Count == 0) {
            canvasItem.Draw -= AfterDraw;
            CANVAS_STYLE_BOXES.Remove(rid);
            RenderingServer.Singleton.CanvasItemSetMaterial(rid, canvasItem.Material?.GetRid() ?? new Rid());
        }
        hasDrawn.Remove(rid);
        materialCopies[rid].Dispose();
        materialCopies.Remove(rid);
        RenderingServer.RequestFrameDrawnCallback(Callable.From(canvasItem.QueueRedraw)); // might not be necessary, but just to be sure (calling QueueRedraw directly will not work as we are technically still drawing)
    }

    // This currently just sets the material of the canvas item, meaning it removes any material that is already set on the canvas item
    // Optimally we would render a mesh as a background with a material set to the mesh,
    // but that seems to not be supported, 2D mesh instances seem to simply ignore the mesh material and only use the canvas item material
    // Source (I am pretty sure this has not yet been fixed): https://github.com/godotengine/godot/issues/51578
    public override void _Draw(Rid toCanvasItem, Rect2 rect) {
        SOME_REF = this;
        EnsurePredrawHooked();
        if (IsInstanceValid(Material)) {
            CanvasItem canvasItem = GetCurrentItemDrawn();
            if (!hasDrawn.ContainsKey(toCanvasItem)) { // hasDrawn contains false for style boxes that have drawn last loop, but will contain nothing on the first draw
                OnFirstDraw(canvasItem);
                if (materialCopies[toCanvasItem] is ShaderMaterial shaderMat) {
                    float time = Time.GetTicksMsec() / 1000f;
                    shaderMat.SetShaderParameter("start_time", time);
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
            materialCopies[rid].Dispose();
            materialCopies[rid] = (Material) Material.Duplicate();
        }
    }
}