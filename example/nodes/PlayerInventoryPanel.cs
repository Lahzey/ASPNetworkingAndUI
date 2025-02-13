using Godot;
using System;
using ASPNetworkingandUI.logic;
using Godot.Collections;

public partial class PlayerInventoryPanel : GridContainer {

    [Export] public PackedScene ItemPanelScene { get; set; }

    private uint currentSlotCount = 0;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (Player.INSTANCE == null) return;
        
        Inventory<Item> inventory = Player.INSTANCE.Inventory;
        Array<Node> children = GetChildren();

        // remove slots if there are too many
        if (inventory.Size < currentSlotCount) {
            for (int i = children.Count - 1; i >= 0; i--) { // backwards looping makes removal easier
                if (children[i] is not ItemPanel itemPanel) continue;
                RemoveChild(itemPanel);
                currentSlotCount--;
                children.RemoveAt(i);
                if (currentSlotCount == inventory.Size) break;
            }
        } else if (inventory.Size > currentSlotCount) {
            for (uint i = currentSlotCount; i < inventory.Size; i++) {
                ItemPanel itemPanel = ItemPanelScene.Instantiate<ItemPanel>();
                AddChild(itemPanel);
                children.Add(itemPanel);
                currentSlotCount++;
            }
        }

        int itemsPerColumn = Math.Max(inventory.ExpansionInterval, 1);
        int columns = Mathf.CeilToInt(inventory.Size / (float) itemsPerColumn);
        Columns = columns;

        int column = 0;
        int row = 0;
        foreach (Node child in children) {
            if (child is not ItemPanel itemPanel) continue;
            itemPanel.Inventory = inventory;
            itemPanel.Index = row + column * itemsPerColumn;
            itemPanel.QueueRedraw();
            column++;
            if (column >= columns) {
                column = 0;
                row++;
            }
        }
    }
}