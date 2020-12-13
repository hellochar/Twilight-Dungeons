using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : ActorController {
  Player player => (Player) actor;

  public override void Update() {
    if (Input.GetKeyDown(KeyCode.V)) {
      player.SetTasks(new SleepTask(player));
    }
    base.Update();
  }

  protected override void HandleActionPerformed(BaseAction action, BaseAction initial) {
    if (action is WaitBaseAction) {
      var waitPrefab = Resources.Load<GameObject>("Effects/Wait");
      var wait = Instantiate(waitPrefab, new Vector3(actor.pos.x, actor.pos.y + 0.9f, 0), Quaternion.identity);
    }
    base.HandleActionPerformed(action, initial);
  }

  public override void OnPointerClick(PointerEventData pointerEventData) {
    // on clicking self, wait for 1 turn
    player.task = new WaitTask(player, 1);
  }
}