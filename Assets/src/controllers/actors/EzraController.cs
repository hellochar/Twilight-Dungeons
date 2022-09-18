using UnityEngine.EventSystems;

public class EzraController : ActorController {
  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (actor.IsDead) {
      return null; // don't do anything to dead actors
    }
    return new SetTasksPlayerInteraction(
      new MoveNextToTargetTask(player, actor.pos),
      new GenericPlayerTask(player, () => {
        // wake ezra up, win the game!!!
        actor.ClearTasks();
        actor.statuses.Add(new SurprisedStatus());
        GameModel.main.GameOver(true);
      })
    );
  }
}