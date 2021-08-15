using UnityEngine.EventSystems;

public class EzraController : ActorController {
  public override void HandleInteracted(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (actor.IsDead) {
      return; // don't do anything to dead actors
    }
    if (player.IsNextTo(actor)) {
      // wake ezra up, win the game!!!
      actor.ClearTasks();
      actor.statuses.Add(new SurprisedStatus());
      GameModel.main.GameOver(true);
    } else {
      player.task = new MoveNextToTargetTask(player, actor.pos);
    }
  }
}