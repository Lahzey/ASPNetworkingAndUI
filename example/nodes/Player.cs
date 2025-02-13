using Godot;
using System;
using ASPNetworkingandUI.logic;

public partial class Player : CharacterBody3D {
    
    [Export] public ItemConfig ItemConfig { get; set; }
    
    // default stuff
    public const float Speed = 5.0f;
    public const float JumpVelocity = 4.5f;
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    // this allows us to accesss the inventory from anywhere
    public static Player INSTANCE { get; private set; }
    public Inventory<Item> Inventory { get; private set; }

    public override void _Ready() {
        Inventory = new Inventory<Item>(9);
        Inventory.AutoExpand = true;
        Inventory.AutoShrink = true;
        Inventory.ExpansionInterval = 9;
        INSTANCE = this;
        
        if (ItemConfig != null) {
            for (int i = 0; i < 6; i++) {
                Inventory.AddItem(new Item(ItemConfig.ItemTypes[i % ItemConfig.ItemTypes.Length]));
            }
        }
    }

    // default stuff
    public override void _PhysicsProcess(double delta) {
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            velocity.Y = JumpVelocity;

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero) {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        } else {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}