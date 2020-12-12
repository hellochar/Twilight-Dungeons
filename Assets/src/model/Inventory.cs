using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Inventory : IEnumerable<Item> {
  public Inventory(Player player, int cap) {
    Player = player;
    items = new Item[cap];
  }

  private Item[] items;
  public Player Player { get; }
  public int capacity => items.Length;
  public virtual Item this[int i] => items[i];

  internal virtual bool AddItem(Item item, int? slotArg = null) {
    if (slotArg == null && item is IStackable stackable) {
      // go through existing stacks and add as much as possible
      foreach (IStackable i in ItemsNonNull().Where(i => i.GetType() == item.GetType())) {
        bool isConsumed = i.Merge(stackable);
        if (isConsumed) {
          item.Destroy();
          return true;
        }
      }
      // if we still exist, then continue
    }

    int? maybeSlot = slotArg ?? GetFirstFreeSlot();
    if (maybeSlot == null) {
      return false;
    }

    int slot = maybeSlot.Value;
    if (items[slot] != null) {
      return false;
    }
    if (item.inventory != null) {
      bool didRemove = item.inventory.RemoveItem(item);
      if (!didRemove) {
        return false;
      }
    }
    items[slot] = item;
    item.inventory = this;
    return true;
  }

  private int? GetFirstFreeSlot() {
    var index = Array.FindIndex(items, 0, items.Length, (t) => t == null);
    if (index < 0) {
      return null;
    }
    return index;
  }

  /// Be careful when calling this method not to lose the item into the nether, unless intentional
  internal bool RemoveItem(Item item) {
    int slot = Array.IndexOf(items, item);
    if (slot < 0) {
      return false;
    }
    items[slot] = null;
    item.inventory = null;
    return true;
  }

  public IEnumerator<Item> GetEnumerator() {
    return ((IEnumerable<Item>)items).GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator() {
    return items.GetEnumerator();
  }

  public IEnumerable<Item> ItemsNonNull() {
    foreach (var item in this) {
      if (item != null) {
        yield return item;
      }
    }
  }

  internal bool HasItem(Item item) {
    return items.Any((i) => i == item);
  }
}