[System.Serializable]
[ObjectInfo(spriteName: "pumpkin-helmet", flavorText: "Protects your noggin from gentle hits, but a hearty thwak will break it.")]
internal class ItemPumpkinHelmet : EquippableItem, IDurable, IAttackDamageTakenModifier, IBodyTakeAttackDamageHandler {
  public override EquipmentSlot slot => EquipmentSlot.Headwear;
  public int durability { get; set; }
  public int maxDurability => 9;

  public ItemPumpkinHelmet() {
    durability = maxDurability;
  }

  public int Modify(int damage) {
    if (damage > 0) {
      this.ReduceDurability();
    }
    return damage - 1;
  }

  internal override string GetStats() => "Blocks 1 damage. If you still take attack damage, the Pumpkin Helmet breaks.";

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    if (damage > 0) {
      Destroy();
    }
  }
}