using System;

[Serializable]
class ItemStick : EquippableItem, IWeapon {
  public static int yieldCost = 3;
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  public override int stacksMax => int.MaxValue;
  // public override bool disjoint => true;

  public ItemStick(int stacks) : base(stacks) { }
  public ItemStick() : base() { }

  public (int, int) AttackSpread => (2, 2);
}
