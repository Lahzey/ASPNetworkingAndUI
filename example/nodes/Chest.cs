using System.Collections.Generic;
using ASPNetworkingandUI.logic;
using Godot;

public partial class Chest : Node3D {
    
    public static Chest INSTANCE { get; private set; }
    
    [Export] public Button OpenButton { get; set; }
    [Export] public AnimationPlayer AnimationPlayer { get; set; }
    
    private List<Item> items = new List<Item>();
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        INSTANCE = this;
        OpenButton.Pressed += () => {
            foreach (Item item in items) {
                Player.INSTANCE.Inventory.AddItem(item);
            }
            AnimationPlayer.Stop(true);
            AnimationPlayer.Play("open");
            
            items.Clear();
            OpenButton.SetText("Empty");
            OpenButton.Disabled = true;
        };
    }
    
    public void AddItem(Item item) {
        if (items.Count == 0) {
            AnimationPlayer.Stop(true);
            AnimationPlayer.Play("close");
            OpenButton.SetText("Open");
            OpenButton.Disabled = false;
        }
        
        items.Add(item);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
