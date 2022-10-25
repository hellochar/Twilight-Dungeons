[System.Serializable]
public class ItemBarkShield : EquippableItem, IAttackDamageTakenModifier {
  public override EquipmentSlot slot => EquipmentSlot.Offhand;
  public override int stacksMax => 6;
  public override bool disjoint => true;

  public int Modify(int damage) {
    if (damage > 0) {
      stacks--;
      return damage - 2;
    } else {
      return damage;
    }
  }


  internal override string GetStats() => "Blocks 2 Damage per hit.";
}
