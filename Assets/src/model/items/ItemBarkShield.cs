public class ItemBarkShield : EquippableItem, IDurable, IDamageTakenModifier {
  public override EquipmentSlot slot => EquipmentSlot.Shield;

  public int durability { get; set; }
  public int maxDurability { get; protected set; }

  public ItemBarkShield() {
    this.maxDurability = 10;
    this.durability = maxDurability;
  }

  public int Modify(int damage) {
    Durables.ReduceDurability(this);
    return damage - 2;
  }


  internal override string GetStats() => "Blocks 2 Damage per hit.";
}
