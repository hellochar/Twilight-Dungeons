using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Pushes you towards the next NerveRoot in the line.")]
public class NerveRoot : Grass, ISteppable {
  public NerveRoot next;

  public float timeNextAction { get; set; }

  public float turnPriority => 9;

  public static bool CanOccupy(Tile tile) => tile is Ground;
  public NerveRoot(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
  }

  public float Step() {
    if (actor != null && next != null && next.pos != null) {
      GameModel.main.EnqueueEvent(() => {
        actor.pos = next.pos;
      });
      OnNoteworthyAction();
    }
    return 1;
  }
}
