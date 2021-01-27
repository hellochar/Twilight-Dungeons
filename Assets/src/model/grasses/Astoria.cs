using System.Linq;
using UnityEngine;

[ObjectInfo(description: "Heals 4 HP on contact.\nConsumed on use.")]
public class Astoria : Grass {
  public Astoria(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
  }

  void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
  }

  private void HandleActorEnter(Actor actor) {
    actor.Heal(4);
    Kill();
  }
}