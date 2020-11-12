using System;
using System.Collections;
using System.Collections.Generic;
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
    inventory.AddItem(new ItemSeed());
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
}

public class Item {
  public virtual string displayName => GetType().Name;
  public Inventory inventory;
  /// remove this item from the inventory
  public void Destroy() {
    if (inventory != null) {
      inventory.RemoveItem(this);
    }
  }
}

class ItemBerries : Item {
  int stacks = 3;
  int stackMax => 10;

  public void Use(Actor a) {
    a.Heal(3);
    stacks--;
    if (stacks == 0) {
      Destroy();
    }
  }
}

public interface IEquippable {
  EquipmentSlot slot { get; }
}

public class ItemBarkShield : Item, IEquippable {
  public EquipmentSlot slot => EquipmentSlot.Shield;

  public void Equip(Player p) {
    p.equipment.Equip(this);
  }
}

public class ItemSeed : Item {
  public void Plant(Soil soil) {
    /// consume this item somehow
    soil.floor.AddActor(new BerryBush(soil.pos));
    Destroy();
  }
}

public enum EquipmentSlot { Head, Shield, Weapon, Body, Feet }

public class Equipment {
  public Dictionary<EquipmentSlot, IEquippable> items = new Dictionary<EquipmentSlot, IEquippable>();

  internal void Equip(IEquippable equippable) {
    var oldItem = items[equippable.slot];
    if (oldItem != null) {
      /// TODO implement
      throw new System.Exception("not implemented");
    }
    items[equippable.slot] = equippable;
  }
}