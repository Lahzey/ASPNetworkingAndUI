using Godot;
using System;
using ASPNetworkingandUI.logic;

public partial class ItemPanel : CenterContainer {

    public Inventory<Item> Inventory { get; set; }
    public int Index { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    public override Variant _GetDragData(Vector2 atPosition) {
        return Index;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) {
        return base._CanDropData(atPosition, data);
    }

    public override void _DropData(Vector2 atPosition, Variant data) {
        base._DropData(atPosition, data);
    }
}