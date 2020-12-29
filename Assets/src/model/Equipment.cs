
/// Equipment is a more specialized inventory where only certain
/// items, namely, IEquippable's, can be AddItem()-ed. 
public class Equipment : Inventory {
  public Equipment(Player player) : base(5) {
    Player = player;
    OnItemAdded += HandleItemAdded;
    OnItemRemoved += HandleItemRemoved;
  }

  private void HandleItemAdded(Item item, Entity source) {
    var e = (EquippableItem) item;
    e.TriggerEquipped(Player);
  }

  private void HandleItemRemoved(Item item) {
    var e = (EquippableItem) item;
    e.TriggerUnequipped(Player);
  }

  public override Item this[int i] {
    get {
      // handle the weapon slot specially - if 
      if (i == (int) EquipmentSlot.Weapon) {
        return base[i] ?? Player.Hands;
      }
      return base[i];
    }
  }

  public Item this[EquipmentSlot e] => this[(int) e];

  public Player Player { get; }

  /// Equipment only allows adding EquippableItems; will return false and do nothing otherwise
  public override bool AddItem(Item item, Entity source = null) {
    if (item is EquippableItem equippable) {
      var slot = (int) equippable.slot;
      return base.AddItem(item, slot, source);
    } else {
      return false;
    }
  }
}

public enum EquipmentSlot {
  Head = 0,
  Weapon = 1,
  Body = 2, 
  Shield = 3,
  Feet = 4
}