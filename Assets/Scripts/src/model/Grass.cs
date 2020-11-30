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

  private void HandleEnterFloor() {
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
