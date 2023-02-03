using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Chases you.\nMust stick next to a wall.")]
public class Wallflower : AIActor {
  // public static Item HomeItem => new ItemPlaceableTile(typeof(Wall));
  public static Item HomeItem => new ItemWallflowerTendril();
  public Wallflower(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 2;
  }

  public static bool CanOccupy(Tile t) => t.CanBeOccupied() && t.floor.GetDiagonalAdjacentTiles(t.pos).Any(n => n is Wall);

  protected override ActorTask GetNextTask() {
    var touchingWalls = floor.GetDiagonalAdjacentTiles(pos).Where(t => t is Wall).ToList();

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

    // var wallNeighborWalls = touchingWalls
    //   .SelectMany(touchingWall => floor.GetCardinalNeighbors(touchingWall.pos, true).Where(t => t is Wall)).Distinct().ToList();
    // var wallNeighborWallNeighbors = wallNeighborWalls
    //   .SelectMany(w => floor.GetDiagonalAdjacentTiles(w.pos)).Distinct().ToList();
    // var occupiableWallNeighborWallNeighbors = wallNeighborWallNeighbors
    //   .Where(t => CanOccupy(t) || t == this.tile).ToList();
    
    // var adjacent = floor.GetDiagonalAdjacentTiles(pos).ToList();
    // var candidateTiles = occupiableWallNeighborWallNeighbors.Intersect(adjacent).ToList();

    var candidateTiles = floor.GetAdjacentTiles(pos).Where(CanOccupy).ToList();

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

[System.Serializable]
[ObjectInfo("wallflower-tendril", description: "Also makes a free attack on the creature directionally behind the one you attack.")]
public class ItemWallflowerTendril : EquippableItem, IWeapon, IActionPerformedHandler {
  public (int, int) AttackSpread => (1, 1);
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final is AttackBaseAction attack) {
      var target = attack.target;
      var attacker = attack.actor;
      var offset = target.pos - attacker.pos;
      if (target.IsNextTo(attacker)) {
        var posBehind = target.pos + offset;
        if (attacker.floor.InBounds(posBehind) && attacker.floor.bodies[posBehind] != null) {
          attacker.Perform(new AttackBaseAction(attacker, attacker.floor.bodies[posBehind], true));
        }
      }
    }
  }
}