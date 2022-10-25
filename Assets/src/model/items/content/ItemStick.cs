using System;

[Serializable]
class ItemStick : EquippableItem, IWeapon {
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  public override int stacksMax => 3;
  public override bool disjoint => true;
  public (int, int) AttackSpread => (2, 2);
}
