[ObjectInfo(spriteName: "pumpkin-helmet", flavorText: "It slows you down but it protects your noggin.")]
internal class ItemPumpkinHelmet : EquippableItem, IDurable, IAttackDamageTakenModifier, IActionCostModifier {
  public override EquipmentSlot slot => EquipmentSlot.Head;
  public int durability { get; set; }
  public int maxDurability { get; protected set; }

  public ItemPumpkinHelmet() {
    this.maxDurability = 30;
    this.durability = maxDurability;
  }

  public int Modify(int damage) {
    this.ReduceDurability();
    return damage - 1;
  }

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.ATTACK] *= 1.5f;
    return input;
  }

  internal override string GetStats() => "Reduces damage taken by 1.\nYou attack 50% slower.";
}