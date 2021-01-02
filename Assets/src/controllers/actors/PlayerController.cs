using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : ActorController {
  Player player => (Player) actor;
  private int deepestDepthVisited = 0;

  public override void Start() {
    base.Start();
    player.OnEnterFloor += HandleEnterFloor;
  }

  private void HandleEnterFloor() {
    var depth = player.floor.depth;
    if (depth > deepestDepthVisited) {
      deepestDepthVisited = depth;
      // Messages.Create("Depth " + depth);
    }
  }

  public override void Update() {
    if (Input.GetKeyDown(KeyCode.V)) {
      player.SetTasks(new SleepTask(player));
    }
    base.Update();
  }

  public void GoHome() {
    if (player.IsInCombat()) {
      Messages.Create("Leave combat to quicktravel home!");
      return;
    }
    var model = GameModel.main;
    if (model.activeFloorIndex == 0) {
      model.PutPlayerAt(model.floors[deepestDepthVisited], false);
      // e.g. floorIndex 2 = depth 3; so we should traverse 3, then 2
      // var floorsToTraverse = GameModel.main.floors.Take(deepestDepthVisited + 1);
      // var tasks = floorsToTraverse.Select((floor) => {
      //   if (floor == model.currentFloor) {
      //     return new MoveToTargetTask(player, floor.downstairs.pos);
      //   }
      //   return new FollowPathTask(
      //     player,
      //     floor.downstairs.pos,
      //     floor.FindPath(floor.upstairs.landing, floor.downstairs.pos, true)
      //   );
      // }).ToArray();
      // player.SetTasks(tasks);
    } else {
      model.PutPlayerAt(model.floors[0], true);
      // var floorsToTraverse = GameModel.main.floors.Skip(1).Take(GameModel.main.activeFloorIndex).Reverse();
      // var tasks = floorsToTraverse.Select((floor) => new MoveToTargetTask(player, floor.upstairs.pos)).ToArray();
      // player.SetTasks(tasks);
    }
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