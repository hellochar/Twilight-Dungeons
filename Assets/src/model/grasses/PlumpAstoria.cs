using System.Linq;
using UnityEngine;

public class PlumpAstoria : Grass {
  public PlumpAstoria(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
  }

  void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
  }

  private void HandleActorEnter(Body actor) {
    actor.Heal(4);
    Kill();
  }
}