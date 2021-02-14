using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Chases you.\nTelegraphs attacks.")]
public class Blob : AIActor {
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority;
  public Blob(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 8;
    faction = Faction.Enemy;
  }

  internal override (int, int) BaseAttackDamage() {
    return (2, 3);
  }

  protected override ActorTask GetNextTask() {
    if (isVisible) {
      if (IsNextTo(GameModel.main.player)) {
        return new AttackGroundTask(this, GameModel.main.player.pos, 1);
      } else {
        return new ChaseTargetTask(this, GameModel.main.player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }
}

[Serializable]
// a boss!
public class BlobBoss : AIActor, ITakeAnyDamageHandler {
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority;
  public BlobBoss(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 32;
    faction = Faction.Enemy;
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
  }

  public void HandleTakeAnyDamage(int damage) {
    if (damage > 0) {
      var tile = floor.BreadthFirstSearch(pos, tile => tile.CanBeOccupied()).Skip(1).FirstOrDefault();
      if (tile != null) {
        var blob = new Blob(tile.pos);
        floor.Put(blob);
      }
    }
  }

  internal override (int, int) BaseAttackDamage() {
    return (3, 4);
  }

  protected override ActorTask GetNextTask() {
    if (isVisible) {
      if (IsNextTo(GameModel.main.player)) {
        // TODO make blob attack a 3x3 area, telegraphs for 2 turns
        return new AttackGroundTask(this, GameModel.main.player.pos, 1);
      } else {
        return new ChaseTargetTask(this, GameModel.main.player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }
}
