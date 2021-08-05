using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Only moves horizontally.\nAttacks anything in its path.", flavorText: "")]
public class Crab : AIActor {
  public override float turnPriority => 40;
  public int dx { get; private set; }
  [field:NonSerialized] /// controller only
  public event Action OnDirectionChanged;

  public Crab(Vector2Int pos) : base(pos) {
    dx = MyRandom.value < 0.5 ? -1 : 1;
    faction = Faction.Neutral;
    hp = baseMaxHp = 7;
  }

  internal override (int, int) BaseAttackDamage() => (2, 2);

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;

    var nextPos = pos + new Vector2Int(dx, 0);
    var nextTile = floor.tiles[nextPos];
    if (nextTile.BasePathfindingWeight() == 0 || nextTile.actor is Crab) {
      // can't walk there; change directions
      dx *= -1;
      OnDirectionChanged?.Invoke();
      return new WaitTask(this, 1);
    } else {
      if (nextTile.body == null) {
        return new MoveToTargetTask(this, nextPos);
      } else {
        // something's blocking the way; attack it
        return new AttackTask(this, nextTile.body);
      }
    }
  }
}
