using System.Collections.Generic;
using UnityEngine;

[ObjectInfo(description: "Only moves horizontally.\nAttacks anything in its path.", flavorText: "")]
public class Crab : AIActor {
  public override float turnPriority => 40;
  private Vector2Int direction = new Vector2Int(UnityEngine.Random.value < 0.5 ? -1 : 1, 0);

  public Crab(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 7;
    ai = AI().GetEnumerator();
  }

  internal override (int, int) BaseAttackDamage() => (2, 3);

  private IEnumerable<ActorTask> AI() {
    var player = GameModel.main.player;
    while (true) {
      var nextPos = pos + direction;
      var nextTile = floor.tiles[nextPos];
      if (nextTile.BasePathfindingWeight() == 0 || nextTile.actor is Crab) {
        // can't walk there; change directions
        direction.x = -1 * direction.x;
        yield return new WaitTask(this, 1).Open();
      } else {
        if (nextTile.body == null) {
          yield return new MoveToTargetTask(this, nextPos).Open();
        } else {
          // something's blocking the way; attack it
          yield return new AttackTask(this, nextTile.actor).Open();
        }
      }
    }
  }
}
