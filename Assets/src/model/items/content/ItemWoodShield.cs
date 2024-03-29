[System.Serializable]
public class ItemBarkShield : EquippableItem, IDurable, IAttackDamageTakenModifier {
  public override EquipmentSlot slot => EquipmentSlot.Offhand;

  public int durability { get; set; }
  public int maxDurability { get; protected set; }

  public ItemBarkShield() {
    this.maxDurability = 6;
    this.durability = maxDurability;
  }

  public int Modify(int damage) {
    if (damage > 0) {
      this.ReduceDurability();
      return damage - 2;
    } else {
      return damage;
    }
  }


  internal override string GetStats() => "Blocks 2 Damage per hit.";
}
