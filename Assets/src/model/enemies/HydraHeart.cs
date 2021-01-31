using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ObjectInfo(description: "Every four turns, spawn a Hydra Head next to the creature closest to it (range 4).\nOn death, all Hydra Heads die as well.\nDoes not attack or move.", flavorText: "Thick veins writhe underneath this pulsating white mass, connecting it to an ever growing network of Heads.")]
public class HydraHeart : AIActor, IBaseActionModifier {
  public static int spawnRange = 4;
  public static bool IsTarget(Body b) => !(b is HydraHead) && !(b is HydraHeart);
  private List<HydraHead> heads = new List<HydraHead>();
  public HydraHeart(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 13;
  }

  private bool needsWait = true;
  protected override ActorTask GetNextTask() {
    if (needsWait) {
      needsWait = false;
      return new WaitTask(this, 3);
    } else {
      needsWait = true;
      return new GenericTask(this, (_) => SpawnHydraHead());
    }
  }

  void SpawnHydraHead() {
    Vector2Int? spawnPos = null;

    var nearestEnemy = floor.ActorsInCircle(pos, spawnRange).Where(IsTarget).OrderBy((a) => a.DistanceTo(pos)).FirstOrDefault();
    if (nearestEnemy != null) {
      spawnPos = Util.RandomPick(floor.GetAdjacentTiles(nearestEnemy.pos).Select(t => t.pos).Where(CanSpawnHydraHeadAt));
    }

    if (spawnPos == null) {
      spawnPos = Util.RandomPick(floor.EnumerateCircle(base.pos, spawnRange).Where(CanSpawnHydraHeadAt));
    }

    if (spawnPos != null) {
      var head = new HydraHead(spawnPos.Value);
      floor.Put(head);
      heads.Add(head);
    }
  }

  private bool CanSpawnHydraHeadAt(Vector2Int p) => floor.tiles[p].CanBeOccupied() && floor.TestVisibility(pos, p);

  public override void HandleDeath() {
    base.HandleDeath();
    GameModel.main.EnqueueEvent(() => {
      foreach (var head in heads) {
        head.Kill();
      }
    });
  }

  internal override (int, int) BaseAttackDamage() => (0, 0);

  public BaseAction Modify(BaseAction input) {
    // cannot move
    if (input.Type == ActionType.MOVE || input.Type == ActionType.ATTACK) {
      return new WaitBaseAction(this);
    }
    return input;
  }
}

[ObjectInfo(description: "Attacks anything adjacent to it.\nStationary.", flavorText: "A fleshy tube with a gaping jaw at the end, grasping at any food nearby.")]
public class HydraHead : AIActor, IBaseActionModifier {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.ATTACK] = 2f,
  };

  protected override ActionCosts actionCosts => StaticActionCosts;
  public HydraHead(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 3;
  }

  internal override (int, int) BaseAttackDamage() => (1, 2);

  protected override ActorTask GetNextTask() {
    var potentialTargets = floor
      .AdjacentBodies(pos)
      .Where(HydraHeart.IsTarget);
    if (potentialTargets.Any()) {
      var target = Util.RandomPick(potentialTargets);
      return new AttackTask(this, target);
    } else {
      return new WaitTask(this, 1);
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