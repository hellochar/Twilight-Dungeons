using System;

[Serializable]
class ItemStick : EquippableItem, IDurable, IWeapon {
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  public int durability { get; set; }

  public int maxDurability => 3;

  public (int, int) AttackSpread => (2, 2);

  public ItemStick() {
    this.durability = maxDurability;
  }
}
