using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grass : SteppableEntity {
  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving grass
    set { }
  }

  public Grass(Vector2Int pos) : base() {
    this._pos = pos;
    OnEnterFloor += HandleEnterFloor;
    timeNextAction = this.timeCreated + 9999;
  }

  protected virtual void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
  }

  void HandleActorEnter(Actor who) {
    who.Heal(1);
    Kill();
  }

  protected override float Step() {
    return 9999;
  }

  public override void Kill() {
    tile.OnActorEnter -= HandleActorEnter;
    OnEnterFloor -= HandleEnterFloor;
    base.Kill();
  }
}

public class Redvines : Grass {
  public Redvines(Vector2Int pos) : base(pos) {
  }

  protected override void HandleEnterFloor() {
    // foreach (var tile in floor.GetAdjacentTiles(pos)) {
    tile.OnActorEnter += HandleAdjacentActorEnter;
    // }
  }

  private void HandleAdjacentActorEnter(Actor who) {
    // stun them for a few turns
    GrappledTask action = new GrappledTask(who, 3, this);
    action.OnDone += HandleActionDone;
    who.InsertTasks(action);
  }

  private void HandleActionDone() {
    if (!IsDead) {
      Kill();
    }
  }

  public override void Kill() {
    // foreach (var tile in floor.GetAdjacentTiles(pos)) {
    /// TODO oh shit you have to manually unregister all your events
    tile.OnActorEnter -= HandleAdjacentActorEnter;
    // }
    base.Kill();
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

public class Mushroom : Grass {
  public Mushroom(Vector2Int pos) : base(pos) {
    timeNextAction = this.timeCreated + 25;
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
  }

  public override void Kill() {
    base.Kill();
  }

  protected override float Step() {
    // find an adjacent square without mushrooms and grow into it
    var noGrassTiles = floor.GetAdjacentTiles(pos).Where((tile) => tile is Ground && tile.grass == null);
    if (noGrassTiles.Any()) {
      var toGrowInto = Util.RandomPick(noGrassTiles);
      var newMushrom = new Mushroom(toGrowInto.pos);
      floor.Add(newMushrom);
    }
    return 25;
  }

}