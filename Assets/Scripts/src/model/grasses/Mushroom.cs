using System.Linq;
using UnityEngine;

public class Mushroom : Grass {
  public Mushroom(Vector2Int pos) : base(pos) {
    timeNextAction = this.timeCreated + GetRandomDuplicateTime();
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
    if (actor == GameModel.main.player) {
      GameModel.main.player.inventory.AddItem(new ItemMushroom(1));
      Kill();
    }
  }

  private float GetRandomDuplicateTime() {
    return UnityEngine.Random.Range(50, 100);
  }

  protected override float Step() {
    // find an adjacent square without mushrooms and grow into it
    var noGrassTiles = floor.GetAdjacentTiles(pos).Where((tile) => tile is Ground && tile.grass == null);
    if (noGrassTiles.Any()) {
      var toGrowInto = Util.RandomPick(noGrassTiles);
      var newMushrom = new Mushroom(toGrowInto.pos);
      floor.Add(newMushrom);
    }
    return GetRandomDuplicateTime();
  }
}