[System.Serializable]
[ObjectInfo(spriteName: "jackal-fur", flavorText: "Patches of matted fur strewn together.")]
public class ItemJackalHide : EquippableItem, IDurable, IAttackDamageTakenModifier {
  public override EquipmentSlot slot => EquipmentSlot.Armor;

  public int durability { get; set; }

  public int maxDurability => 4;

  public ItemJackalHide() {
    durability = maxDurability;
  }

  public int Modify(int input) {
    return input - 1;
  }

  internal override string GetStats() => "Blocks 1 damage.";
}