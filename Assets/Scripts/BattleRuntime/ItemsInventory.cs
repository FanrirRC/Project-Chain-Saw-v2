using System.Collections.Generic;
using UnityEngine;
using Data;

public class ItemsInventory : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public ItemDefinition item;
        public int count = 1;
    }

    public List<Entry> items = new();

    public bool TryConsume(ItemDefinition item, int amount = 1)
    {
        if (!item) return false;
        var e = items.Find(x => x.item == item);
        if (e == null || e.count < amount) return false;
        e.count -= amount;
        if (e.count <= 0) items.Remove(e);
        return true;
    }

    public void Add(ItemDefinition item, int amount = 1)
    {
        if (!item || amount <= 0) return;
        var e = items.Find(x => x.item == item);
        if (e == null) items.Add(new Entry { item = item, count = amount });
        else e.count += amount;
    }
}
