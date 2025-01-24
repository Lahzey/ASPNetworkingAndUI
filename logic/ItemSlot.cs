#nullable enable
namespace ASPNetworkingandUI.logic;

public class ItemSlot<T> where T : class {

    private T? item;
    
    public ItemSlot(T? item) {
        this.item = item;
    }

    public T? GetItem() {
        return item;
    }

    public void SetItem(T? item) {
        this.item = item;
    }
    
}