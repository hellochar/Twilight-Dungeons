[System.Serializable]
[ObjectInfo(spriteName: "jackal-fur", flavorText: "Patches of matted fur strewn together.")]
public class ItemJackalHide : EquippableItem, IAttackDamageTakenModifier {
  public static int yieldCost = 4;
  public override EquipmentSlot slot => EquipmentSlot.Armor;
  public override int stacksMax => int.MaxValue;

  public ItemJackalHide() {
  }

  public int Modify(int input) {
    stacks--;
    return input - 1;
  }

  internal override string GetStats() => "Blocks 1 damage.";
}