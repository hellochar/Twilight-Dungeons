using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo("butterfly", "")]
public class ItemButterfly : Item, IUsable {
  public void Use(Actor a) {
    var tile = Util.RandomPick(a.floor.GetAdjacentTiles(a.pos).Where((t) => t.CanBeOccupied()));
    if (tile != null) {
      a.floor.Put(new Butterfly(tile.pos));
      Destroy();
    }
  }
}

[System.Serializable]
[ObjectInfo(description: "Every 5 turns, Butterfly will duplicate the Grass you're standing on to the cardinally adjacent squares.")]
public class Butterfly : AIActor {

  private static float DUPLICATE_CD = 5;
  float cooldown = 0;

  public Butterfly(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    hp = baseMaxHp = 1;
    statuses.Add(new CharmedStatus());
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;

    // we want to duplicate
    if (cooldown <= 0 && player.grass != null) {
      // we're on top of a grass, duplicate!
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, DuplicateGrass));
    }
    // we can't duplicate, either because there's nothing nearby or we're on CD
    if (cooldown > 0) {
      cooldown--;
    }
    return new ChaseTargetTask(this, player);
  }

  private void DuplicateGrass() {
    var grass = GameModel.main.player.grass;
    if (grass != null) {
      var neighborTiles = grass.floor.GetCardinalNeighbors(grass.pos).Where((tile) => tile is Ground && tile.grass == null);
      var constructorInfo = grass.GetType().GetConstructor(new System.Type[1] { typeof(Vector2Int) });
      foreach (var tile in neighborTiles) {
        var newGrass = (Grass)constructorInfo.Invoke(new object[] { tile.pos });
        grass.floor.Put(newGrass);
      }
      cooldown = DUPLICATE_CD;
    }
  }
}
