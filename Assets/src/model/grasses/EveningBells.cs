using UnityEngine;

internal class EveningBells : Grass {
  public static bool CanOccupy(Tile tile) => tile is Ground;
  public EveningBells(Vector2Int pos) : base(pos) {
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
    if (actor != GameModel.main.player) {
      actor.SetTasks(new SleepTask(actor, 3, true));
      Kill();
    }
  }
}
