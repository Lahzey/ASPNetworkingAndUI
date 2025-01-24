using System;
using System.Collections.Generic;
using Godot;

[GlobalClass, Tool, Icon("res://ui/styleboxes/base/StyleBoxMaterial.svg")]
public partial class StyleBoxCompound : StyleBox {

    [Export] public StyleBox StyleBoxA { get; set; }
    [Export] public StyleBox StyleBoxB { get; set; }
    
    public override void _Draw(Rid toCanvasItem, Rect2 rect) {
        StyleBoxA?.Draw(toCanvasItem, rect);
        StyleBoxB?.Draw(toCanvasItem, rect);
    }
}