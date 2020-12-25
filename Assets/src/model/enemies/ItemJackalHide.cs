[ObjectInfo(spriteName: "jackal-fur", flavorText: "Patches of matted fur strewn together.")]
public class ItemJackalHide : EquippableItem, IMaxHPModifier {
  public override EquipmentSlot slot => EquipmentSlot.Body;

  public ItemJackalHide() {}

  public int Modify(int input) {
    return input + 4;
  }

  internal override string GetStats() => "+4 max HP.";
}