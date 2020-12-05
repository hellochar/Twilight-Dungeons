using UnityEngine;

public class SoftGrass : Grass {
  public SoftGrass(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  void HandleEnterFloor() {
    /// TODO make this declarative instead of manually registering events
    tile.OnActorEnter += HandleActorEnter;
    tile.OnActorLeave += HandleActorLeave;
  }

  private void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
    tile.OnActorLeave -= HandleActorLeave;
  }

  void HandleActorEnter(Actor who) {
    if (who is Player player) {
      player.statuses.Add(new SoftGrassStatus());
    }
  }

  void HandleActorLeave(Actor who) {
    who.statuses.RemoveOfType<SoftGrassStatus>();
  }

  public override void Kill() {
    if (tile.actor != null) {
      HandleActorLeave(tile.actor);
    }
    base.Kill();
  }
}
