using System;
using Godot;

namespace ASPNetworkingandUI.logic;

public class Inventory<T> where T : class {
    public int Size => itemSlots.Length;

    public bool AutoExpand { get; set; }
    public bool AutoShrink { get; set; }
    public int ExpansionInterval { get; set; }

    private int minSize;
    private ItemSlot<T>[] itemSlots;

    public Inventory(int initialSize) {
        minSize = initialSize;
        itemSlots = new ItemSlot<T>[initialSize];
        for (int i = 0; i < initialSize; i++) {
            itemSlots[i] = new ItemSlot<T>(null);
        }
    }

    private int CountFilledSlots() {
        int filledSlots = 0;
        foreach (ItemSlot<T> slot in itemSlots) {
            if (slot.GetItem() != null) filledSlots++;
        }

        return filledSlots;
    }

    private bool TryExpand() {
        if (!AutoExpand) return false;
        int interval = Math.Max(ExpansionInterval, 1);
        Resize(Size + interval);
        return true;
    }

    private bool TryShrink() {
        if (!AutoShrink) return false;
        int interval = Math.Max(ExpansionInterval, 1);
        int emtpySlots = Size - CountFilledSlots();
        bool hasShrunk = false;
        while (interval >= emtpySlots) {
            Resize(Size - interval);
            emtpySlots -= interval;
            hasShrunk = true;
        }
        return hasShrunk;
    }

    public void Resize(int newSize) {
        if (newSize == Size) return;
        ItemSlot<T>[] newSlots = new ItemSlot<T>[newSize];
        if (newSize > Size) { // expanding inventory
            // copy all existing slots
            Array.Copy(itemSlots, newSlots, itemSlots.Length);
            
            // add new slots
            for (int i = itemSlots.Length; i < newSize; i++) {
                newSlots[i] = new ItemSlot<T>(null);
            }
        } else { // shrinking inventory
            int filledSlots = CountFilledSlots();
            
            // drop any items that won't fit in the new inventory size (starting from the end)
            while (filledSlots < newSize) {
                for (int i = itemSlots.Length - 1; i >= 0; i--) {
                    if (itemSlots[i].GetItem() == null) continue;
                    DropItemAt(i, true);
                    filledSlots--;
                    goto EndWhile; // c# does not support continuing (or breaking) outer loops, so this is a workaround
                }
                throw new Exception("Failed to find empty slots to drop, CountFilledSlots is likely returning an incorrect result."); // if we don't do this we end up in an infinite loop
                EndWhile:
                continue;
            }
            
            // if new size is bigger than the filled slots, we keep some empty slots at the start
            int emptySlotsToKeep = newSize - filledSlots;
            int newIndex = 0;
            foreach (ItemSlot<T> slot in itemSlots) {
                if (slot.GetItem() == null) {
                    if (emptySlotsToKeep <= 0) continue;
                    emptySlotsToKeep--;
                }
                newSlots[newIndex] = slot; // if we exceed array bounds here that means emptySlotsToKeep is calculated incorrectly, so getting an exception is preferable
                newIndex++;
            }
        }
        
        itemSlots = newSlots;
    }

    public void DropItemAt(int index, bool suppressShrink = false) {
        T item = GetItem(index);
        if (item is Item itemInstance) Chest.INSTANCE.AddItem(itemInstance);
        itemSlots[index].SetItem(null); // here we could add some spawning logic
        if (!suppressShrink) TryShrink();
    }

    public bool AddItem(T item, bool allowExpansion = true) {
        for (int i = 0; i < Size; i++) {
            ItemSlot<T> slot = itemSlots[i];
            if (slot.GetItem() != null) continue;
            slot.SetItem(item);
            return true;
        }
        if (!allowExpansion) return false;

        if (TryExpand()) {
            return AddItem(item, false); // now it should fit, but block further expansion attempts just in case to prevent infinite loops
        }
        
        return false;
    }
    
    public T? GetItem(int index) {
        if (index >= Size) throw new IndexOutOfRangeException();
        return itemSlots[index].GetItem();
    }
    
    public T? ReplaceItem(int index, T? item) {
        if (index >= Size) throw new IndexOutOfRangeException();
        T? oldItem = itemSlots[index].GetItem();
        itemSlots[index].SetItem(item);
        return oldItem;
    }
}