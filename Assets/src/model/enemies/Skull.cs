using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Chases you.")]
public class Skull : AIActor {
  public Skull(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 1;
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        return new ChaseTargetTask(this, player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  internal override (int, int) BaseAttackDamage() => (2, 2);
}

[System.Serializable]
[ObjectInfo(description: "Doesn't move.")]
public class Octopus : AIActor {
  public Octopus(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 3;
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        return new WaitTask(this, 4);
      }
    } else {
      return new WaitTask(this, 4);
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 2);
}

[System.Serializable]
[ObjectInfo(description: "Creates another mouse when it attacks.")]
public class Mouse : AIActor, IActionPerformedHandler {
  public Mouse(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 2;
    SetTasks(new SleepTask(this, 3, true));
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.ATTACK) {
      floor.Put(new Mouse(pos));
      SetTasks(new SleepTask(this, 3, true));
    }
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        return new ChaseTargetTask(this, player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 2);
}