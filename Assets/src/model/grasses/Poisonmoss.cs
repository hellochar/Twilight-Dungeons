using System.Linq;
using UnityEngine;

class Poisonmoss : Grass {
  public static bool CanOccupy(Tile tile) => tile is Ground;
  private bool hasDuplicated = false;
  public Poisonmoss(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
  }

  protected override float Step() {
    if (actor != null) {
      TriggerNoteworthyAction();
      actor.statuses.Add(new PoisonedStatus(1));
    }
    if (!hasDuplicated) {
      var tile = Util.RandomPick(floor
        .GetAdjacentTiles(pos)
      /// take over non-poisonmoss tiles!
      .Where(t => CanOccupy(t) && t.grass != null && !(t.grass is Poisonmoss))
      );
      if (tile != null) {
        TriggerNoteworthyAction();
        hasDuplicated = true;
        tile.grass.Kill();
        floor.Put(new Poisonmoss(tile.pos));
      }
    }
    return 1;
  }
}