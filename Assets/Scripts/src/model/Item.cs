using System;
using System.Collections.Generic;
using System.Linq;

public class Item {
  public virtual string displayName {
    get {
      /// get rid of the "Item" prefix
      var typeNameNoSpaces = GetType().Name.Substring(4);
      // add spaces
      var name = string.Concat(typeNameNoSpaces.Select(x => Char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
      return name;
    }
  }

  public Inventory inventory;
  /// remove this item from the inventory
  public void Destroy() {
    if (inventory != null) {
      inventory.RemoveItem(this);
    }
  }

  internal virtual string GetStatsString() => "No stats.";
}

interface IStackable {
  int stacks { get; set; }
  int stacksMax { get; }
}

class ItemBerries : Item, IStackable {
  private int _stacks;

  public ItemBerries(int stacks) {
    this.stacks = stacks;
  }

  public int stacksMax => 10;

  public int stacks { get => _stacks; set => _stacks = value; }

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