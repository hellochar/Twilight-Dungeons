using System;
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
    GrappledAction action = new GrappledAction(who, 3, this);
    action.OnDone += HandleActionDone;
    who.InsertActions(action);
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

public class GrappledAction : WaitAction {
  public Entity grappler { get; }
  public GrappledAction(Actor actor, int turns, Entity grappler) : base(actor, turns) {
    this.grappler = grappler;
  }

  public override bool IsDone() {
    if (grappler.IsDead) {
      return true;
    }
    return base.IsDone();
  }
}