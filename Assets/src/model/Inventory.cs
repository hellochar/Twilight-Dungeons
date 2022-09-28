using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Inventory : IEnumerable<Item> {
  private Item[] items;
  public int capacity => items.Length;

  public virtual Item this[int i] => items[i];
  [field:NonSerialized] /// Controller only
  public event Action<Item, Entity> OnItemAdded;
  [field:NonSerialized] /// Controller only
  public event Action<Item> OnItemRemoved;
  [NonSerialized] // controller only
  public Action<Item> OnItemDestroyed = delegate { };

  public Inventory(params Item[] items) {
    this.items = items;
  }

  public Inventory(int cap) : this(new Item[cap]) { }

  internal virtual bool AddItem(Item item, int slot, Entity source = null) {
    if (item is IStackable stackable) {
      // go through existing stacks and add as much as possible
      foreach (IStackable i in ItemsNonNull().Where(i => i.GetType() == item.GetType())) {
        bool isConsumed;
        // if we cannot stack; go on
        if (item is IConditionallyStackable c1 && i is IConditionallyStackable c2 && !c1.CanStackWith(c2)) {
          isConsumed = false;
        } else {
          isConsumed = i.Merge(stackable);
        }
        if (isConsumed) {
          item.Destroy();
          HandleItemAdded(item, source);
          return true;
        }
      }
      // if we still exist, then continue
    }

    if (slot == -1) {
      return false;
    }

    /// two options:
    /// no other inventory -> just put it into this inventory
    /// yes other inventory -> swap whatever's in the current slot into that one

    var otherInventory = item.inventory;
    if (otherInventory != null) {
      var otherInventorySlot = Array.IndexOf(otherInventory.items, item);
      otherInventory.RemoveItem(item);
      var itemToSwap = items[slot];
      if (itemToSwap != null) {
        RemoveItem(itemToSwap);
        otherInventory.AddItem(itemToSwap, otherInventorySlot);
      }
    }
    items[slot] = item;
    item.inventory = this;
    HandleItemAdded(item, source);
    return true;
  }

  public virtual bool AddItem(Item item, Entity source = null, bool expandToFit = false) {
    var slot = GetFirstFreeSlot();
    if (expandToFit && !slot.HasValue) {
      Array.Resize(ref items, items.Length + 1);
      slot = capacity - 1;
    }

    if (!(item is IStackable) && slot == null) {
      return false;
    } else {
      return AddItem(item, slot ?? -1, source);
    }
  }

  protected virtual void HandleItemAdded(Item item, Entity source) {
    OnItemAdded?.Invoke(item, source);
  }

  protected virtual void HandleItemRemoved(Item item) {
    OnItemRemoved?.Invoke(item);
  }

  internal void TryDropAllItems(Floor floor, Vector2Int pos) {
    // it pops out randomly adjacent
    foreach (var item in ItemsNonNull()) {
      var tile = floor.tiles[pos];
      TryDropItem(floor, tile.pos, item);
    }
  }

  public void TryDropItem(Floor floor, Vector2Int pos, Item item) {
    // bool IsTileFreeForItem(Tile t) => t.item == null && t.actor == null && t.BasePathfindingWeight() != 0;

    // var tile = floor.tiles[pos];
    // if (!IsTileFreeForItem(tile)) {
    //   tile = Util.RandomPick(floor.GetAdjacentTiles(pos).Where(IsTileFreeForItem));
    // }

    // if (tile != null) {
      DropItemImpl(floor, pos, item);
    // }
  }

  private void DropItemImpl(Floor floor, Vector2Int pos, Item item) {
    RemoveItem(item);
    var itemOnGround = new ItemOnGround(pos, item, pos);
    floor.Put(itemOnGround);
  }

  private int? GetFirstFreeSlot() {
    var index = Array.FindIndex(items, 0, items.Length, (t) => t == null);
    if (index == -1) {
      return null;
    }
    return index;
  }

  /// Be careful when calling this method not to lose the item into the nether, unless intentional
  internal virtual bool RemoveItem(Item item) {
    int slot = Array.IndexOf(items, item);
    if (slot < 0) {
      return false;
    }
    items[slot] = null;
    item.inventory = null;
    HandleItemRemoved(item);
    return true;
  }

  public int GetSlotFor(Item item) {
    return Array.IndexOf(items, item);
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