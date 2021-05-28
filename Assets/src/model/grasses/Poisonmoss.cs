using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Applies Poison to the creature standing over it every turn.\nGradually turns adjacent Grass into Poisonmoss.\nDies if surrounded by Walls or other Poisonmoss.")]
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
    /// entirety is filled with poisonmoss
    if (floor.GetAdjacentTiles(pos).Where(t => t.grass is Poisonmoss || t.BasePathfindingWeight() == 0).Count() == 9) {
      // do this immediately; otherwise many poisonmoss will die at once
      KillSelf();
    } else if (age % 6 == 5) {
      // every 6 turns, grow or die.
      var tile = Util.RandomPick(floor
        .GetAdjacentTiles(pos)
        /// take over non-poisonmoss tiles!
        .Where(t => CanOccupy(t) && t.grass != null && !(t.grass is Poisonmoss))
      );
      if (tile != null) {
        // grow
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