
/// Equipment is a more specialized inventory where only certain
/// items, namely, IEquippable's, can be AddItem()-ed. 
public class Equipment : Inventory {
  public Equipment(Player player) : base(player, 5) {
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

  internal override bool AddItem(Item item, int? slotArg = null) {
    if (item is EquippableItem equippable) {
      var slot = (int) equippable.slot;
      return base.AddItem(item, slot);
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