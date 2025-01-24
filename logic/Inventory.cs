using System;

namespace ASPNetworkingandUI.logic;

public class Inventory<T> where T : class {
    public uint Size => (uint) itemSlots.Length;

    public bool AutoExpand { get; set; }
    public bool AutoShrink { get; set; }
    public uint ExpansionInterval { get; set; }

    private uint minSize;
    private ItemSlot<T>[] itemSlots;

    public Inventory(uint initialSize) {
        minSize = initialSize;
        itemSlots = new ItemSlot<T>[initialSize];
        for (int i = 0; i < initialSize; i++) {
            itemSlots[i] = new ItemSlot<T>(null);
        }
    }

    private uint CountFilledSlots() {
        uint filledSlots = 0;
        foreach (ItemSlot<T> slot in itemSlots) {
            if (slot.GetItem() != null) filledSlots++;
        }

        return filledSlots;
    }

    private bool TryExpand() {
        if (!AutoExpand) return false;
        uint interval = Math.Max(ExpansionInterval, 1);
        Resize(Size + interval);
        return true;
    }

    private bool TryShrink() {
        if (!AutoShrink) return false;
        uint interval = Math.Max(ExpansionInterval, 1);
        uint emtpySlots = Size - CountFilledSlots();
        bool hasShrunk = false;
        while (interval >= emtpySlots) {
            Resize(Size - interval);
            emtpySlots -= interval;
            hasShrunk = true;
        }
        return hasShrunk;
    }

    public void Resize(uint newSize) {
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
            uint filledSlots = CountFilledSlots();
            
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
            uint emptySlotsToKeep = newSize - filledSlots;
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
    }

    private void DropItemAt(int index, bool surpressShrink = false) {
        itemSlots[index].SetItem(null); // here we could add some spawning logic
        if (!surpressShrink) TryShrink();
    }

    public bool PickupItem(T item) {
        for (int i = 0; i < Size; i++) {
            ItemSlot<T> slot = itemSlots[i];
            if (slot.GetItem() != null) continue;
            slot.SetItem(item);
            return true;
        }

        if (TryExpand()) {
            return PickupItem(item); // now it should fit
        }
        
        return false;
    }
}