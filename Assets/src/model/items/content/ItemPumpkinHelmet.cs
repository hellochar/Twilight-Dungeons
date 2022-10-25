[System.Serializable]
[ObjectInfo(spriteName: "pumpkin-helmet", flavorText: "Protects your noggin from gentle hits, but a hearty thwak will break it.")]
internal class ItemPumpkinHelmet : EquippableItem, IAttackDamageTakenModifier, IBodyTakeAttackDamageHandler {
  public override EquipmentSlot slot => EquipmentSlot.Headwear;
  public override int stacksMax => 5;
  public override bool disjoint => true;

  public ItemPumpkinHelmet() {
  }

  public int Modify(int damage) {
    if (damage > 0) {
      stacks--;
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