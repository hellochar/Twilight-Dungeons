using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HydraHeart : AIActor, IBaseActionModifier {
  public static bool IsTarget(Actor a) => !(a is HydraHead) && !(a is HydraHeart);
  private List<HydraHead> heads = new List<HydraHead>();
  public HydraHeart(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 13;
    ai = AI().GetEnumerator();
    OnDeath += HandleDeath;
  }

  private IEnumerable<ActorTask> AI() {
    while(true) {
      yield return new WaitTask(this, 3);
      yield return new GenericTask(this, (_) => SpawnHydraHead());
    }
  }

  void SpawnHydraHead() {
    Vector2Int? spawnPos = null;

    var nearestEnemy = floor.ActorsInCircle(pos, 3).Where(IsTarget).OrderBy((a) => a.DistanceTo(pos)).FirstOrDefault();
    if (nearestEnemy != null) {
      spawnPos = Util.RandomPick(floor.GetAdjacentTiles(nearestEnemy.pos).Select(t => t.pos).Where(CanSpawnHydraHeadAt));
    }

    if (spawnPos == null) {
      spawnPos = Util.RandomPick(floor.EnumerateCircle(base.pos, 3).Where(CanSpawnHydraHeadAt));
    }

    if (spawnPos != null) {
      var head = new HydraHead(spawnPos.Value);
      floor.Put(head);
      heads.Add(head);
    }
  }

  private bool CanSpawnHydraHeadAt(Vector2Int p) => floor.tiles[p].CanBeOccupied() && floor.TestVisibility(pos, p);

  private void HandleDeath() {
    GameModel.main.EnqueueEvent(() => {
      foreach (var head in heads) {
        head.Kill();
      }
    });
  }

  internal override int BaseAttackDamage() {
    return UnityEngine.Random.Range(1, 3);
  }

  public BaseAction Modify(BaseAction input) {
    // cannot move
    if (input.Type == ActionType.MOVE || input.Type == ActionType.ATTACK) {
      return new WaitBaseAction(this);
    }
    return input;
  }
}

public class HydraHead : AIActor, IBaseActionModifier {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.ATTACK] = 2f,
  };

  protected override ActionCosts actionCosts => StaticActionCosts;
  public HydraHead(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 2;
    ai = AI().GetEnumerator();
  }

  internal override int BaseAttackDamage() {
    return UnityEngine.Random.Range(1, 3);
  }

  private IEnumerable<ActorTask> AI() {
    while (true) {
      var potentialTargets = floor
        .AdjacentActors(pos)
        .Where(HydraHeart.IsTarget);
      if (potentialTargets.Any()) {
        var target = Util.RandomPick(potentialTargets);
        yield return new AttackTask(this, target);
      } else {
        yield return new WaitTask(this, 1);
      }
    }
  }

  public BaseAction Modify(BaseAction input) {
    // cannot move
    if (input.Type == ActionType.MOVE) {
      return new WaitBaseAction(this);
    }
    return input;
  }
}