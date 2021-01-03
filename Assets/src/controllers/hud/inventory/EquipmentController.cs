public class EquipmentController : InventoryController {
  public override void Start() {
    if (inventory == null) {
      inventory = GameModel.main.player.equipment;
    }
  }
}
