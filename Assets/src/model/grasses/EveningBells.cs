using UnityEngine;

[ObjectInfo(description: "Any non-player Creature walking into the Evening Bells falls into Deep Sleep for 3 turns. This consumes the Evening Bells.")]
public class EveningBells : Grass {
  public readonly float angle;

  public static bool CanOccupy(Tile tile) => tile is Ground;
  public EveningBells(Vector2Int pos, float angle = 0) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
    this.angle = angle;
  }

  void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
  }

  void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
  }

  private void HandleActorEnter(Actor actor) {
    if (actor != GameModel.main.player) {
      actor.SetTasks(new SleepTask(actor, 3, true));
      Kill();
    }
  }
}
