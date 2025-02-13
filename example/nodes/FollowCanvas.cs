using System.Collections.Generic;
using ASPNetworkingandUI.ui.scripts;
using Godot;

[GlobalClass]
public partial class FollowCanvas : Node3D {
    
    [Export] public AttachmentPoint Attachment { get; set; }

    private readonly List<Control> controlChildren = new List<Control>();
    private readonly Dictionary<CanvasItem, Vector2> initialPositions = new Dictionary<CanvasItem, Vector2>();
    
    public enum AttachmentPoint {
        TOP_LEFT,
        TOP_CENTER,
        TOP_RIGHT,
        CENTER_LEFT,
        CENTER,
        CENTER_RIGHT,
        BOTTOM_LEFT,
        BOTTOM_CENTER,
        BOTTOM_RIGHT,
    }

    public override void _Ready() {
        ReloadControlChildren();
        ChildOrderChanged += ReloadControlChildren;
    }

    private void ReloadControlChildren() {
        controlChildren.Clear();
        foreach (Node child in GetChildren()) {
            if (child is Control control) {
                controlChildren.Add(control);
            }
        }
    }

    public override void _Process(double delta) {
        Vector2 viewportPosition = GetViewport().GetCamera3D().UnprojectPosition(GlobalPosition);
        foreach (Control control in controlChildren) {
            if (!initialPositions.ContainsKey(control)) {
                initialPositions[control] = control.Position;
            }
            
            switch (Attachment) {
                case AttachmentPoint.TOP_LEFT:
                    control.Position = initialPositions[control] + viewportPosition;
                    break;
                case AttachmentPoint.TOP_CENTER:
                    control.Position = initialPositions[control] + viewportPosition - new Vector2(control.Size.X / 2, 0);
                    break;
                case AttachmentPoint.TOP_RIGHT:
                    control.Position = initialPositions[control] + viewportPosition - new Vector2(control.Size.X, 0);
                    break;
                case AttachmentPoint.CENTER_LEFT:
                    control.Position = initialPositions[control] + viewportPosition - new Vector2(0, control.Size.Y / 2);
                    break;
                case AttachmentPoint.CENTER:
                    control.Position = initialPositions[control] + viewportPosition - control.Size / 2;
                    break;
                case AttachmentPoint.CENTER_RIGHT:
                    control.Position = initialPositions[control] + viewportPosition - new Vector2(control.Size.X, control.Size.Y / 2);
                    break;
                case AttachmentPoint.BOTTOM_LEFT:
                    control.Position = initialPositions[control] + viewportPosition - new Vector2(0, control.Size.Y);
                    break;
                case AttachmentPoint.BOTTOM_CENTER:
                    control.Position = initialPositions[control] + viewportPosition - new Vector2(control.Size.X / 2, control.Size.Y);
                    break;
                case AttachmentPoint.BOTTOM_RIGHT:
                    control.Position = initialPositions[control] + viewportPosition - control.Size;
                    break;
            }
        }
    }
}