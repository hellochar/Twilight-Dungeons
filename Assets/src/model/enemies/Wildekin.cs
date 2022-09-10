using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Chases you.\nStays one Tile away from Walls or non-Wildekins, but will attack you if possible.\nRuns away for three turns after it attacks.", flavorText: "")]
public class Wildekin : AIActor, IAttackHandler {
  public override float turnPriority => 60;
  public Wildekin(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 7;
  }

  internal override (int, int) BaseAttackDamage() => (3, 3);

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        // move one step closer
        var tiles = AdjacentTilesInPreferenceOrder();
        if (tiles.Any()) {
          var bestScore = TilePreference(tiles.First());
          var tile = tiles.Where(t => TilePreference(t) == bestScore)
            .OrderBy((t) => t.DistanceTo(GameModel.main.player))
            .First();
          return new MoveToTargetTask(this, tile.pos);
        } else {
          return new WaitTask(this, 1);
        }
      }
    } else {
      var tiles = AdjacentTilesInPreferenceOrder();
      if (tiles.Any()) {
        // tile preference version of moving randomly
        var bestScore = TilePreference(tiles.First());
        var tile = Util.RandomPick(tiles.Where(t => TilePreference(t) == bestScore));
        return new MoveToTargetTask(this, tile.pos);
      } else {
        return new WaitTask(this, 1);
      }
    }
  }

  public IEnumerable<Tile> AdjacentTilesInPreferenceOrder() => floor
    .GetAdjacentTiles(pos)
    .Where(t => (t.CanBeOccupied() || t == this.tile))
    .OrderBy(TilePreference);

  // lower = more preferred
  public int TilePreference(Tile t) => floor
    .GetAdjacentTiles(t.pos)
    .Where(t2 =>
      // Avoid walls
      t2 is Wall ||
      // Avoid non Wildekins
      (t2.body != null && !(t2.body is Wildekin))
    ).Count()
    // never walk directly into the Player
    + (t.IsNextTo(GameModel.main.player) ? 100 : 0);

  public void OnAttack(int damage, Body target) {
    if (target is Actor a) {
      SetTasks(new RunAwayTask(this, target.pos, 3, false));
    }
  }
}
