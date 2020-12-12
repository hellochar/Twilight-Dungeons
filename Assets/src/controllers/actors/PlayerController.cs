using UnityEngine.EventSystems;

public class PlayerController : ActorController {
  Player player => (Player) actor;

  public override void OnPointerClick(PointerEventData pointerEventData) {
    // on clicking self, wait for 1 turn
    player.task = new WaitTask(player, 1);
  }
}