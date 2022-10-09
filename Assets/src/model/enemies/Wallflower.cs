using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Chases you.\nMust stick next to a wall.")]
public class Wallflower : AIActor {
  public Wallflower(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 3;
  }

  public static bool CanOccupy(Tile t) => t.CanBeOccupied() && t.floor.GetCardinalNeighbors(t.pos).Any(n => n is Wall);

  protected override ActorTask GetNextTask() {
    var touchingWalls = floor.GetCardinalNeighbors(pos).Where(t => t is Wall).ToList();

    if (!touchingWalls.Any()) {
      // oh god! walk randomly until you are touching
      return new MoveRandomlyTask(this);
    }

    var player = GameModel.main.player;
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      }
    }

    var wallNeighborWalls = touchingWalls
      .SelectMany(touchingWall => floor.GetCardinalNeighbors(touchingWall.pos, true).Where(t => t is Wall)).Distinct().ToList();
    var wallNeighborWallNeighbors = wallNeighborWalls
      .SelectMany(w => floor.GetCardinalNeighbors(w.pos)).Distinct().ToList();
    var occupiableWallNeighborWallNeighbors = wallNeighborWallNeighbors
      .Where(t => CanOccupy(t) || t == this.tile).ToList();
    
    var adjacent = floor.GetAdjacentTiles(pos).ToList();
    var candidateTiles = occupiableWallNeighborWallNeighbors.Intersect(adjacent).ToList();

    var nextTile = candidateTiles.OrderBy((t) => t.DistanceTo(GameModel.main.player)).FirstOrDefault();

    if (nextTile == this.tile) {
      return new WaitTask(this, 1);
    } else if (nextTile != null) {
      return new MoveToTargetTask(this, nextTile.pos);
    } else {
      return new WaitTask(this, 1);
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);
}
