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
    var reduction = input - 1;
    this.ReduceDurability();
    return reduction;
  }

  internal override string GetStats() => "Blocks 1 damage.";
}