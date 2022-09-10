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

[System.Serializable]
[ObjectInfo(description: "Applies Vulnerable when it hits you.\nGets stunned when attacked.", flavorText: "")]
public class Dizapper : AIActor, IAttackHandler, IBodyTakeAttackDamageHandler {
  public override float turnPriority => 60;
  public Dizapper(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 9;
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);

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

  public void OnAttack(int damage, Body target) {
    if (target is Actor a) {
      a.statuses.Add(new VulnerableStatus(10));
    }
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    if (damage > 0) {
      SetTasks(new WaitTask(this, 1));
      statuses.Add(new SurprisedStatus());
    }
  }
}

[System.Serializable]
[ObjectInfo(description: "When attacked, it duplicates into two with half HP.", flavorText: "")]
public class Goo : AIActor, IBodyTakeAttackDamageHandler {
  public override float turnPriority => 50;
  public Goo(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 20;
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);

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

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    if (damage > 0) {
      SetTasks(new TelegraphedTask(this, 1, new GenericBaseAction(this, Split)));
    }
  }

  void Split() {
    var hp1 = Mathf.CeilToInt(hp / 2f);
    var hp2 = Mathf.FloorToInt(hp / 2f);
    var goo1 = new Goo(pos);
    goo1.hp = hp1;
    goo1.ClearTasks();
    var goo2 = new Goo(pos);
    goo2.hp = hp2;
    goo2.ClearTasks();
    floor.PutAll(goo1, goo2);
    KillSelf();
  }
}

[System.Serializable]
[ObjectInfo(description: "If the Hardshell would take 3 or more attack damage, that is instead reduced to 0.", flavorText: "")]
public class HardShell : AIActor, IAttackDamageTakenModifier {
  public override float turnPriority => 50;
  public HardShell(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 9;
  }

  internal override (int, int) BaseAttackDamage() => (2, 2);

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

  public int Modify(int input) {
    if (input >= 3) {
      input = 0;
    }
    return input;
  }
}
