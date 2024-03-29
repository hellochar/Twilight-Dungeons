
/// Equipment is a more specialized inventory where only certain
/// items, namely, IEquippable's, can be AddItem()-ed. 
[System.Serializable]
public class Equipment : Inventory {
  public Equipment(Player player) : base(5) {
    Player = player;
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
      // ensure there isn't a sticky item in the existing slot
      if (this[slot] is ISticky) {
        throw new CannotPerformActionException(this[slot].displayName + " is stuck to your body!");
        // return false;
      }
      return base.AddItem(item, slot, source);
    } else {
      return false;
    }
  }

  protected override void HandleItemAdded(Item item, Entity source) {
    base.HandleItemAdded(item, source);
    var e = item as EquippableItem;
    e.OnEquipped();
  }

  protected override void HandleItemRemoved(Item item) {
    base.HandleItemRemoved(item);
    var e = item as EquippableItem;
    e.OnUnequipped();
  }
}

public enum EquipmentSlot {
  Headwear = 0,
  Weapon = 1,
  Armor = 2, 
  Offhand = 3,
  Footwear = 4
}