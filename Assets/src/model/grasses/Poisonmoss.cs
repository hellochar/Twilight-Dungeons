using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Applies Poison to the creature standing over it every turn.\nTurns adjacent Grass into Poisonmoss.")]
class Poisonmoss : Grass, ISteppable {
  public static bool CanOccupy(Tile tile) => tile is Ground;

  public Poisonmoss(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
  }

  public float Step() {
    if (actor != null) {
      OnNoteworthyAction();
      actor.statuses.Add(new PoisonedStatus(1));
    }
    // every 3 turns, grow
    if (age % 3 == 2) {
      var tile = Util.RandomPick(floor
        .GetAdjacentTiles(pos)
        /// take over non-poisonmoss tiles!
        .Where(t => CanOccupy(t) && t.grass != null && !(t.grass is Poisonmoss))
      );
      if (tile != null) {
        OnNoteworthyAction();
        tile.grass.Kill(this);
        floor.Put(new Poisonmoss(tile.pos));
      }
    }
    return 1;
  }

  public float timeNextAction { get; set; }
  public float turnPriority => 50;
}