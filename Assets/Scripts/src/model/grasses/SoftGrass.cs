using UnityEngine;

public class SoftGrass : Grass {
  public SoftGrass(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  void HandleEnterFloor() {
    /// TODO make this declarative instead of manually registering events
    tile.OnActorEnter += HandleActorEnter;
  }

  private void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
  }

  void HandleActorEnter(Actor who) {
    if (who is Player player) {
      if (!player.statuses.Has<SoftGrassStatus>()) {
        player.statuses.Add(new SoftGrassStatus(player));
      }
    }
  }
}
