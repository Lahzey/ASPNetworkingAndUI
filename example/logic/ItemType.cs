using Godot;

namespace ASPNetworkingandUI.logic;

[GlobalClass]
public partial class ItemType : Resource {
    
    [Export] public string Name { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Texture2D Icon { get; set; }
    
    [Export] public ItemType CraftedFromA { get; set; }
    [Export] public ItemType CraftedFromB { get; set; }
    
}