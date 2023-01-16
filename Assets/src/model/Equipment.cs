
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

  public EquippableItem this[EquipmentSlot e] => this[(int) e] as EquippableItem;

  public Player Player { get; }

  /// Equipment only allows adding EquippableItems; will return false and do nothing otherwise
  public override bool AddItem(Item item, Entity source = null, bool expandToFit = false) {
    if (item is EquippableItem equippable) {
      var slot = equippable.slot;
      // ensure there isn't a sticky item in the existing slot
      if (this[slot] is ISticky) {
        throw new CannotPerformActionException(this[slot].displayName + " is stuck to your body!");
        // return false;
      }
#if experimental_autoequip
      // we already have an Equipment there; drop it on the floor
      EquippableItem existingItem = this[slot];
      if (this[slot] != null) {
        if (item.CanStackWith(existingItem)) {
          bool isConsumed = existingItem.Merge(item);
          if (isConsumed) {
            item.Destroy();
            HandleItemAdded(item, source);
            return true;
          } else {
            // there's still some leftover; go the false route so backup still happens on item
            return false;
          }
        } else {
          // we're a different type of thing; drop the existing item first
          if (!(existingItem is ItemHands)) {
            existingItem.Drop(Player);
          }
        }
      }
#endif
      return base.AddItem(item, (int) slot, source);
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