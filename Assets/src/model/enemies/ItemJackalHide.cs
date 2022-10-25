[System.Serializable]
[ObjectInfo(spriteName: "jackal-fur", flavorText: "Patches of matted fur strewn together.")]
public class ItemJackalHide : EquippableItem, IAttackDamageTakenModifier {
  public override EquipmentSlot slot => EquipmentSlot.Armor;

  public override int stacksMax => 4;
  public override bool disjoint => true;

  public ItemJackalHide() {
  }

  public int Modify(int input) {
    return input - 1;
  }

  internal override string GetStats() => "Blocks 1 damage.";
}