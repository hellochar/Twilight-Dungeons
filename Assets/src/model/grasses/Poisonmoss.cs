using System.Linq;
using UnityEngine;

class Poisonmoss : Grass, ISteppable {
  public static bool CanOccupy(Tile tile) => tile is Ground;
  private bool hasDuplicated = false;

  public Poisonmoss(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
  }

  public float Step() {
    if (actor != null) {
      OnNoteworthyAction();
      actor.statuses.Add(new PoisonedStatus(1));
    }
    if (!hasDuplicated) {
      var tile = Util.RandomPick(floor
        .GetAdjacentTiles(pos)
      /// take over non-poisonmoss tiles!
      .Where(t => CanOccupy(t) && t.grass != null && !(t.grass is Poisonmoss))
      );
      if (tile != null) {
        OnNoteworthyAction();
        hasDuplicated = true;
        tile.grass.Kill();
        floor.Put(new Poisonmoss(tile.pos));
      }
    }
    return 1;
  }

  public float timeNextAction { get; set; }
  public float turnPriority => 50;
}