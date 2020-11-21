using System;
using System.Collections.Generic;
using System.Linq;

public class Item {
  public virtual string displayName =>
  /// get rid of the "Item" prefix
    Util.WithSpaces(GetType().Name.Substring(4));

  public Inventory inventory;
  /// remove this item from the inventory
  public void Destroy(Actor a) {
    if (inventory != null) {
      inventory.RemoveItem(this);
    }
  }

  internal virtual string GetStats() => "No stats.";

  public virtual List<ActorAction> GetAvailableActions(Player player) {
    return new List<ActorAction> {
      new GenericAction(player, Destroy)
    };
  }
}

interface IStackable {
  int stacks { get; set; }
  int stacksMax { get; }

  /// remove newStacks from this stack and return a new
  /// stackable with that many stacks
  IStackable Split(int newStacks);

  /// Merge as many stacks as possible from other into this one.
  /// May call Destroy() on the other stack. Return true if the other stack is now empty
  /// NOTE: it's the responsibility of the caller to then call Destroy() on the IStackable!
  bool Merge(IStackable other);
}

class ItemBerries : Item, IStackable {

  public ItemBerries(int stacks) {
    if (stacks <= 0) {
      throw new ArgumentException("Made a 0 or negative stack of berries!");
    }
    this.stacks = stacks;
  }

  public int stacksMax => 10;

  public int stacks { get; set; }


  public IStackable Split(int newStacks) {
    if (newStacks >= stacks) {
      throw new ArgumentException($"Cannot split a stack of {stacks} into one of {newStacks}!");
    }
    var newItem = new ItemBerries(newStacks);
    stacks -= newStacks;
    return newItem;
  }

  public bool Merge(IStackable other) {
    var spaceLeft = stacksMax - stacks;
    var stacksToAdd = UnityEngine.Mathf.Clamp(other.stacks, 0, spaceLeft);
    stacks += stacksToAdd;
    other.stacks -= stacksToAdd;
    return other.stacks == 0;
  }

  public void Use(Actor a) {
    a.Heal(3);
    if (a is Player player) {
      player.IncreaseFullness(0.05f);
    }
    stacks--;
    if (stacks == 0) {
      Destroy(a);
    }
  }

  public override List<ActorAction> GetAvailableActions(Player player) {
    var actions = base.GetAvailableActions(player);
    actions.Add(new GenericAction(player, Use));
    return actions;
  }

  internal override string GetStats() => "Heals 3 HP.\nRecover 5% hunger.";
}

public interface IEquippable {
  EquipmentSlot slot { get; }
}

public interface IDurable {
  int durability { get; set; }
  int maxDurability { get; }
}

public class ItemBarkShield : Item, IEquippable, IDurable, IDamageModifier {
  public EquipmentSlot slot => EquipmentSlot.Shield;

  public int durability { get; set; }
  public int maxDurability { get; protected set; }

  public ItemBarkShield() {
    this.maxDurability = 10;
    this.durability = maxDurability;
  }

  public int ModifyDamage(int damage) {
    this.ReduceDurability();
    return damage - 2;
  }

  public void ReduceDurability() {
    this.durability--;
    if (durability <= 0) {
      this.Destroy(null);
    }
  }

  public void Equip(Actor a) {
    ((Player)a).equipment.AddItem(this);
  }

  public void Unequip(Actor a) {
    ((Player)a).inventory.AddItem(this);
  }

  public override List<ActorAction> GetAvailableActions(Player actor) {
    var actions = base.GetAvailableActions(actor);
    if (actor.inventory.HasItem(this)) {
      actions.Add(new GenericAction(actor, Equip));
    } else if (actor.equipment.HasItem(this)) {
      actions.Add(new GenericAction(actor, Unequip));
    }
    return actions;
  }

  internal override string GetStats() => $"Blocks 2 Damage per hit.\nDurability: {durability}/{maxDurability}.";
}

public class ItemSeed : Item {
  public void Plant(Soil soil) {
    /// consume this item somehow
    soil.floor.AddActor(new BerryBush(soil.pos));
    Destroy(null);
  }
}

public enum EquipmentSlot {
  Head = 0,
  Weapon = 1,
  Body = 2, 
  Shield = 3,
  Feet = 4
}

/// Equipment is a more specialized inventory where only certain
/// items, namely, IEquippable's, can be AddItem()-ed. 
public class Equipment : Inventory {
  public Equipment(Player player) : base(player, 5) {
  }

  public Item this[EquipmentSlot e] => this[(int) e];

  internal override bool AddItem(Item item, int? slotArg = null) {
    if (item is IEquippable equippable) {
      var slot = (int) equippable.slot;
      return base.AddItem(item, slot);
    } else {
      return false;
    }
  }
}