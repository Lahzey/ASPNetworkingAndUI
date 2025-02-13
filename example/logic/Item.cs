using Godot;

namespace ASPNetworkingandUI.logic;

public class Item {

    public ItemType Type { get; set; }

    public Item(ItemType type) {
        Type = type;
    }
    
}