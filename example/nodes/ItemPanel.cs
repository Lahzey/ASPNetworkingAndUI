using Godot;
using System;
using ASPNetworkingandUI.logic;

public partial class ItemPanel : PanelContainer, ItemDraggable {
    
    [Export] public StyleBox CraftHoverStyle { get; set; }

    private TextureRect icon;
    private StyleBoxDecorated styleBox;
    
    private Color defaultBorderColor;
    private Color hoverBorderColor;
    private Color dropAcceptBorderColor;
    private Color dropRefuseBorderColor;
    
    public Inventory<Item> Inventory { get; set; }
    public int Index { get; set; }

    private DateTime? craftStartedAt = null;
    
    public override void _Ready() {
        icon = GetNode<TextureRect>("Icon");
        if (GetThemeStylebox("panel") is StyleBoxDecorated decoratedStyleBox) {
            styleBox = (StyleBoxDecorated)decoratedStyleBox.Duplicate(true);
            AddThemeStyleboxOverride("panel", styleBox); // makes the style box unique to each node, allowing them to control color individually
            defaultBorderColor = styleBox.BackgroundPolygon.BorderColor;
            hoverBorderColor = defaultBorderColor.Lightened(0.25f);
            dropAcceptBorderColor = defaultBorderColor.Lerp(Colors.Green, 0.2f);
            dropRefuseBorderColor = defaultBorderColor.Lerp(Colors.Red, 0.2f);
        } else {
            GD.PrintErr("Could not find stylebox 'panel'");
        }

        MouseEntered += () => {
            bool? canDrop = CanDropCurrentData();
            if (canDrop == null) {
                styleBox.BackgroundPolygon.BorderColor = hoverBorderColor;
                return;
            }

            if (canDrop.Value) {
                ItemDraggable itemDraggable = (ItemDraggable)GetViewport().GuiGetDragData().AsGodotObject();
                ItemType? craftingTarget = GetCratingTargetWith(itemDraggable.GetInventory().GetItem(itemDraggable.GetIndex()));
                if (craftingTarget != null) {
                    SetShowCratingPreview(true, craftingTarget);
                } else {
                    styleBox.BackgroundPolygon.BorderColor = dropAcceptBorderColor;
                }
            } else {
                styleBox.BackgroundPolygon.BorderColor = dropRefuseBorderColor;
            }
        };
        MouseExited += () => {
            styleBox.BackgroundPolygon.BorderColor = defaultBorderColor;
            SetShowCratingPreview(false);
        };
    }

    public override void _GuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseButton) {
            if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed) {
                Item item = Inventory.GetItem(Index);
                if (item == null) return;
                ContextMenu.INSTANCE.ShowContextMenu(GetGlobalMousePosition(), new ContextMenuItem[] {
                    ContextMenuItem.Label("Drop", () => {
                        Inventory.DropItemAt(Index);
                    }),
                    ContextMenuItem.Label("Duplicate", () => {
                        Inventory.AddItem(new Item(item.Type));
                    })
                });
                GetViewport().SetInputAsHandled();
            }
        }
    }
    
    private void SetShowCratingPreview(bool show, ItemType? craftingTarget = null) {
        if (!show && craftStartedAt == null) return;
        if (show && craftStartedAt != null) return;
        
        if (show) {
            AddThemeStyleboxOverride("panel", CraftHoverStyle);
            craftStartedAt = DateTime.Now;
            CraftingPreview.INSTANCE.OpenPreview(craftingTarget);
            CraftingPreview.INSTANCE.Position = (Vector2I) (GetGlobalPosition() + new Vector2(Size.X + 5, 0));
        } else {
            AddThemeStyleboxOverride("panel", styleBox);
            craftStartedAt = null;
            CraftingPreview.INSTANCE.ClosePreview();
        }
    }

    private ItemType? GetCratingTargetWith(Item with) {
        Item item = Inventory.GetItem(Index);
        if (item == null || with == null) return null;

        return Player.INSTANCE.ItemConfig.GetRecipeFor(item.Type, with.Type);
    }

    private bool? CanDropCurrentData() {
        Variant dragData = GetViewport().GuiGetDragData();
        if (dragData.VariantType == Variant.Type.Nil) return null;
        
        if (_CanDropData(GetLocalMousePosition(), dragData)) {
            if (dragData.AsGodotObject() == this) return null;
            else return true;
        } else {
            return false;
        }
    }

    public override void _Process(double delta) {
        icon.SetTexture(Inventory?.GetItem(Index)?.Type.Icon);
    }

    public Inventory<Item> GetInventory() {
        return Inventory;
    }

    public int GetIndex() {
        return Index;
    }

    public override Variant _GetDragData(Vector2 atPosition) {
        if (Inventory.GetItem(Index) == null) return (GodotObject) null;
        TextureRect dragPreviewTexture = new TextureRect();
        dragPreviewTexture.SetTexture(icon.Texture);
        dragPreviewTexture.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        
        Control dragPreview = new Control();
        dragPreview.AddChild(dragPreviewTexture);
        dragPreviewTexture.Size = Size;
        dragPreviewTexture.Position = -GetLocalMousePosition();
        
        SetDragPreview(dragPreview);

        icon.Modulate = Colors.Transparent;
        
        return this;
    }

    public override void _Notification(int what) {
        switch ((long) what) { // why does it pass an int when the notification types are stored as longs???
            case NotificationDragEnd:
                icon.Modulate = Colors.White;
                if (styleBox.BackgroundPolygon.BorderColor == dropAcceptBorderColor || styleBox.BackgroundPolygon.BorderColor == dropRefuseBorderColor) {
                    styleBox.BackgroundPolygon.BorderColor = hoverBorderColor;
                }
                SetShowCratingPreview(false);
                break;
        }
    }

    public override string _GetTooltip(Vector2 atPosition) {
        return Inventory.GetItem(Index)?.Type.Name;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) {
        try {
            GodotObject obj = data.AsGodotObject();
            return obj is ItemDraggable;
        } catch (Exception e) {
            return false;
        }
    }

    public override void _DropData(Vector2 atPosition, Variant data) {
        GodotObject obj = data.AsGodotObject();
        if (!(obj is ItemDraggable draggable)) return;
        
        if (craftStartedAt != null && (DateTime.Now - craftStartedAt.Value).TotalSeconds >= 2) {
            bool craftSuccess = CraftWith(draggable);
            if (!craftSuccess) GD.PrintErr("Failed to craft item, no recipe found.");
        } else {
            SwapWith(draggable);
        }
    }

    private bool CraftWith(ItemDraggable draggable) {
        Item item = Inventory.GetItem(Index);
        Item otherItem = draggable.GetInventory().GetItem(draggable.GetIndex());
        
        ItemType targetType = Player.INSTANCE.ItemConfig.GetRecipeFor(item.Type, otherItem.Type);
        if (targetType == null) return false;
        
        Item newItem = new Item(targetType);
        Inventory.ReplaceItem(Index, newItem);
        draggable.GetInventory().ReplaceItem(draggable.GetIndex(), null);
        return true;
    }

    private void SwapWith(ItemDraggable draggable) {
        Inventory<Item> sourceInventory = draggable.GetInventory();
        int sourceIndex = draggable.GetIndex();

        Item? newItem = sourceInventory.GetItem(sourceIndex);
        Item? oldItem = Inventory.ReplaceItem(Index, newItem);
        sourceInventory.ReplaceItem(sourceIndex, oldItem);
    }
}