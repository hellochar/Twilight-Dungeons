public class MatchInventoryItemSlotState : MatchItemSlotState {
  int slotIndex;
  private Inventory inventory;
  public override Item item => inventory[slotIndex];

  public override void Start() {
    inventory = GameModel.main.player.inventory;
    slotIndex = transform.GetSiblingIndex();
    base.Start();
  }
}
