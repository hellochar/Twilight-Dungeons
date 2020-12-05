using System.Collections.Generic;
using UnityEngine;

public class Redvines : Grass {
  public Redvines(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  private void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
  }

  private void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
  }

  private void HandleActorEnter(Actor who) {
    // grapple them for a few turns
    GrappledTask action = new GrappledTask(who, 3, this);
    action.OnDone += HandleActionDone;
    who.InsertTasks(action);
  }

  private void HandleActionDone() {
    if (!IsDead) {
      Kill();
    }
  }
}

public class GrappledTask : ActorTask {
  private readonly int turns;
  public Entity grappler { get; }
  public GrappledTask(Actor actor, int turns, Entity grappler) : base(actor) {
    this.turns = turns;
    this.grappler = grappler;
  }

  public override IEnumerator<BaseAction> Enumerator() {
    for (var i = 0; i < turns; i++) {
      if (grappler.IsDead) {
        yield break;
      }
      yield return new WaitBaseAction(actor);
    }
  }
}