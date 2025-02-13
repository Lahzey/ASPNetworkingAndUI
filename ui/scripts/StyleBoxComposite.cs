using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass, Tool, Icon("res://ui/icons/StyleBoxMaterial.svg")]
public partial class StyleBoxComposite : StyleBox {

    private StyleBox styleBoxA;
    [Export] public StyleBox StyleBoxA { get => styleBoxA; set { styleBoxA = value; AutoSetContentMargins(); } }

    private StyleBox styleBoxB;
    [Export] public StyleBox StyleBoxB { get => styleBoxB; set { styleBoxB = value; AutoSetContentMargins(); } }
    
    public override void _Draw(Rid toCanvasItem, Rect2 rect) {
        StyleBoxA?.Draw(toCanvasItem, rect);
        StyleBoxB?.Draw(toCanvasItem, rect);
    }

    public override void _ValidateProperty(Dictionary property) {
        StringName stringName = property["name"].AsStringName();
        if (stringName == StyleBox.PropertyName.ContentMarginLeft || stringName == StyleBox.PropertyName.ContentMarginRight || stringName == StyleBox.PropertyName.ContentMarginTop || stringName == StyleBox.PropertyName.ContentMarginBottom) {
            property["usage"] = (int) (property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly);
        }
    }

    private void AutoSetContentMargins() {
        ContentMarginLeft = Math.Max(StyleBoxA?.ContentMarginLeft ?? 0, StyleBoxB?.ContentMarginLeft ?? 0);
        ContentMarginRight = Math.Max(StyleBoxA?.ContentMarginRight ?? 0, StyleBoxB?.ContentMarginRight ?? 0);
        ContentMarginTop = Math.Max(StyleBoxA?.ContentMarginTop ?? 0, StyleBoxB?.ContentMarginTop ?? 0);
        ContentMarginBottom = Math.Max(StyleBoxA?.ContentMarginBottom ?? 0, StyleBoxB?.ContentMarginBottom ?? 0);
        EmitChanged();
    }
}