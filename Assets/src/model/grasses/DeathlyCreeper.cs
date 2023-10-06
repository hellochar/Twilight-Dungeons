using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Spreads to unoccupied adjacent Tiles without Grass.\n\nIf all Ground tiles have Black Creeper, all creatures (including you) Die.")]
public class DeathlyCreeper : Grass, /*IActorEnterHandler,*/ ISteppable {
  public override string displayName => "Black Creeper";
  public static bool CanOccupy(Tile tile) => tile.CanBeOccupied() && tile is Ground && !(tile.grass is DeathlyCreeper) /*&& tile.grass == null*/;
  public DeathlyCreeper(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
  }

  static float timeLastChecked = -1f;

  public float timeNextAction { get; set; }

  public float turnPriority => 50;

  public float Step() {
    if (timeLastChecked != GameModel.main.time) {
      CheckWin();
    }

    // if (actor != null) {
    //   OnNoteworthyAction();
    //   actor.TakeDamage(1, this);
    // }
    if (age % 4 == 3) {
      var adjacent = Util.RandomPick(floor.GetCardinalNeighbors(pos).Where(CanOccupy));
      if (adjacent != null) {
        OnNoteworthyAction();
        floor.Put(new DeathlyCreeper(adjacent.pos));
      }
    }
    return 1;
  }

  private void CheckWin() {
    bool isLevelCovered = floor.tiles.OfType<Ground>().All(tile => tile.grass is DeathlyCreeper);
    if (isLevelCovered) {
      GameModel.main.EnqueueEvent(() => {
        // copy body list in case it's modified
        foreach (var body in floor.bodies.ToList()) {
          body.Kill(this);
        }
      });
    }
  }
  // public void HandleActorEnter(Actor who) {
  //   who.TakeDamage(1, this);
  //   // who.Kill(this);
  //   Kill(who);
  // }
}
