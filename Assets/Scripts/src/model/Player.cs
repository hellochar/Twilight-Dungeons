using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class Player : Actor {
  /// called when the player's action is set to something not null
  public Inventory inventory { get; }
  public Equipment equipment { get; }

  internal override float queueOrderOffset => 0f;

  public Player(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    inventory = new Inventory(this, 12);
    inventory.AddItem(new ItemSeed(), 4);
    inventory.AddItem(new ItemBarkShield(), 5);
    inventory.AddItem(new ItemSeed(), 6);
    inventory.AddItem(new ItemBerries(3), 7);
    inventory.AddItem(new ItemSeed(), 8);
    hp = 9;
    hpMax = 12;
  }

  public override Vector2Int pos {
    get {
      return base.pos;
    }

    set {
      GameModel model = GameModel.main;
      if (floor != null) {
        floor.RemoveVisibility(this);
      }
      base.pos = value;
      if (floor != null) {
        floor.AddVisibility(this);
        Tile t = floor.tiles[value.x, value.y];
        model.EnqueueEvent(() => t.OnPlayerEnter());
      }
    }
  }

  // internal async Task WaitUntilActionIsDecided() {
  //   while(action == null) {
  //     await Task.Delay(16);
  //   }
  //   return;
  // }
}

public class Inventory : IEnumerable<Item> {
  public Inventory(Player player, int cap) {
    Player = player;
    items = new Item[cap];
  }

  private Item[] items;
  public Player Player { get; }
  public int capacity => items.Length;
  public Item this[int i] => items[i];

  internal bool AddItem(Item item, int? slotArg = null) {
    if (slotArg == null && item is IStackable stackable) {
      // go through existing stacks and add as much as possible
      foreach (IStackable i in ItemsNonNull().Where(i => i.GetType() == item.GetType())) {
        bool isConsumed = i.Merge(stackable);
        if (isConsumed) {
          item.Destroy(null);
          return true;
        }
      }
      // if we still exist, then continue
    }

    int slot = slotArg ?? GetFirstFreeSlot();
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

  private int GetFirstFreeSlot() {
    return Array.FindIndex(items, 0, items.Length, (t) => t == null);
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
}