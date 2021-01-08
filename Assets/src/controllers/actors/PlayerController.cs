using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : ActorController {
  Player player => (Player) actor;

  public override void Update() {
    if (Input.GetKeyDown(KeyCode.V)) {
      player.SetTasks(new SleepTask(player));
    }
    if (Input.GetKeyDown(KeyCode.Space)) {
      player.floor.ForceAddVisibility(player.floor.EnumerateFloor());
    }
    var model = GameModel.main;
    if (Input.GetKeyDown(KeyCode.Equals)) {
      GameModel.main.PutPlayerAt(model.floors[model.activeFloorIndex + 1], false);
    } else if (Input.GetKeyDown(KeyCode.Minus)) {
      GameModel.main.PutPlayerAt(model.floors[model.activeFloorIndex - 1], false);
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

  public override void PointerClick(PointerEventData pointerEventData) {
    if (player.task != null) {
      player.ClearTasks();
    } else {
      // on clicking self, wait for 1 turn
      player.task = new WaitTask(player, 1);
    }
  }
}