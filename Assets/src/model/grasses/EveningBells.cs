using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Any non-player Creature walking into the Evening Bells falls into Deep Sleep for 3 turns. This consumes the Evening Bells.")]
public class EveningBells : Grass, IActorEnterHandler {
  public readonly float angle;

  public static bool CanOccupy(Tile tile) => tile is Ground && tile.CanBeOccupied();
  public EveningBells(Vector2Int pos, float angle) : base(pos) {
    this.angle = angle;
  }
  public EveningBells(Vector2Int pos) : this(pos, 0) {}

  public void HandleActorEnter(Actor actor) {
    if (actor != GameModel.main.player) {
      actor.SetTasks(new SleepTask(actor, 3, true));
      GameModel.main.EnqueueEvent(() => Kill(actor));
    }
  }
}
