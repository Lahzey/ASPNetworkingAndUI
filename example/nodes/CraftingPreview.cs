using ASPNetworkingandUI.logic;
using Godot;

public partial class CraftingPreview : Panel {
    
    public static CraftingPreview INSTANCE { get; private set; }

    [Export] public TextureRect Icon { get; set; }
    [Export] public RichTextLabel Label { get; set; }
    [Export] public AnimationPlayer AnimationPlayer { get; set; }
    
    public void OpenPreview(ItemType itemType) {
        Icon.Texture = itemType.Icon;
        Label.Text = $"[center][b][color=CE935F]{itemType.Name}[/color][/b][/center]\n\n{itemType.Description}";
        if (AnimationPlayer.IsPlaying()) AnimationPlayer.Stop();
        AnimationPlayer.Play("open");
    }

    public void ClosePreview() {
        if (AnimationPlayer.IsPlaying()) AnimationPlayer.Stop();
        AnimationPlayer.Play("close");
    }
    
    public override void _Ready() {
        INSTANCE = this;
    }
}
