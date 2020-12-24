using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : IEnumerable<Item> {
  private Item[] items;
  public int capacity => items.Length;
  public virtual Item this[int i] => items[i];
  public event Action<Item, Entity> OnItemAdded;

  public Inventory(params Item[] items) {
    this.items = items;
  }

  public Inventory(int cap) : this(new Item[cap]) { }

  internal virtual bool AddItem(Item item, int slot, Entity source = null) {
    if (item is IStackable stackable) {
      // go through existing stacks and add as much as possible
      foreach (IStackable i in ItemsNonNull().Where(i => i.GetType() == item.GetType())) {
        bool isConsumed = i.Merge(stackable);
        if (isConsumed) {
          item.Destroy();
          OnItemAdded?.Invoke(item, source);
          return true;
        }
      }
      // if we still exist, then continue
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
    OnItemAdded?.Invoke(item, source);
    return true;
  }

  public virtual bool AddItem(Item item, Entity source = null) {
    var slot = GetFirstFreeSlot();
    if (!(item is IStackable) && slot == null) {
      return false;
    } else {
      return AddItem(item, slot.GetValueOrDefault(), source);
    }
  }

  internal void DropRandomlyOntoFloorAround(Floor floor, Vector2Int pos) {
    // it pops out randomly adjacent
    foreach (var item in ItemsNonNull()) {
      var tile = floor.tiles[pos];
      if (!IsTileFreeForItem(tile)) {
        tile = Util.RandomPick(floor.GetAdjacentTiles(pos).Where(IsTileFreeForItem));
      }
      if (tile != null) {
        DropItem(item, tile.pos);
      }
    }

    bool IsTileFreeForItem(Tile tile) => tile.item == null && tile.actor == null && tile.BasePathfindingWeight() != 0;

    void DropItem(Item item, Vector2Int p) {
      RemoveItem(item);
      var itemOnGround = new ItemOnGround(p, item, pos);
      floor.Put(itemOnGround);
    }
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