using System.Collections.Generic;
using UnityEngine;

[ObjectInfo(description: "Only moves horizontally.\nAttacks anything in its path.", flavorText: "")]
public class Crab : AIActor {
  public override float turnPriority => 40;
  private Vector2Int direction = new Vector2Int(UnityEngine.Random.value < 0.5 ? -1 : 1, 0);

  public Crab(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 7;
  }

  internal override (int, int) BaseAttackDamage() => (2, 3);

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;

    var nextPos = pos + direction;
    var nextTile = floor.tiles[nextPos];
    if (nextTile.BasePathfindingWeight() == 0 || nextTile.actor is Crab) {
      // can't walk there; change directions
      direction.x = -1 * direction.x;
      return new WaitTask(this, 1).Open();
    } else {
      if (nextTile.body == null) {
        return new MoveToTargetTask(this, nextPos).Open();
      } else {
        // something's blocking the way; attack it
        return new AttackTask(this, nextTile.actor).Open();
      }
    }
  }
}
