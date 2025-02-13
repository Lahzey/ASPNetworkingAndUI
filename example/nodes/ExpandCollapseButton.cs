using Godot;

public partial class ExpandCollapseButton : Button {

    private bool expanded = true;
    
    [Export] public Control Target { get; set; }
    [Export] public float CollapsedX { get; set; }
    [Export] public float ExpandedX { get; set; }
    [Export] public AnimationPlayer AnimationPlayer { get; set; }

    private float startX;
    
    private float animationProgress;
    [Export] public float AnimationProgress {
        get => animationProgress;
        set {
            animationProgress = value;
            if (Target == null) return;
            float width = Target.Size.X;
            float targetX = expanded ? ExpandedX : (CollapsedX - width);
            Target.SetPosition(new Vector2(Mathf.Lerp(startX, targetX, animationProgress), Target.Position.Y));
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        // hook on click event
        Pressed += () => {
            if (Target == null) return;
            expanded = !expanded;
            startX = Target.Position.X;
            SetText(expanded ? "<" : ">");
            animationProgress = 0;
            AnimationPlayer.Stop(true);
            AnimationPlayer.Play("animate");
        };
    }
}