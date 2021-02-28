[System.Serializable]
[ObjectInfo(spriteName: "goop", flavorText: "Thick, gluey Blob innards are pasted onto your feet, but it feels great.")]
public class ItemGloopShoes : EquippableItem, ISticky, IDurable, IBaseActionModifier {
  public override EquipmentSlot slot => EquipmentSlot.Footwear;
  private int turnsLeft = 50;

  public ItemGloopShoes() {
    durability = maxDurability;
  }

  public int durability { get; set; }
  public int maxDurability => 4;

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.MOVE) {
      if (turnsLeft <= 0) {
        GameModel.main.player.Heal(1);
        turnsLeft = 50;
        this.ReduceDurability();
      }
      turnsLeft--;
    }
    return input;
  }

  internal override string GetStats() => $"Cannot be unequipped.\nEvery 50 turns ({turnsLeft} left), heal 1 HP.";
}