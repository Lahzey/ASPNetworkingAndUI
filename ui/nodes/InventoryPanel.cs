using Godot;
using System;
using ASPNetworkingandUI.logic;
using Godot.Collections;

public partial class InventoryPanel : GridContainer {

    [Export] public PackedScene ItemPanelScene { get; set; }

    private uint currentSlotCount = 0;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (Player.PLAYER == null) return;
        
        Inventory<Item> inventory = Player.PLAYER.Inventory;
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

        int inventoryIndex = 0;
        foreach (Node child in children) {
            if (child is not ItemPanel itemPanel) continue;
            itemPanel.Inventory = inventory;
            itemPanel.Index = inventoryIndex;
            itemPanel.QueueRedraw();
            inventoryIndex++;
        }
    }
}