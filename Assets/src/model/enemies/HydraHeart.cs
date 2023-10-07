using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Every four turns, spawns a Hydra Head (max 12) within range 4.\nOn death, all Hydra Heads die as well.\nDoes not move or attack.", flavorText: "Thick veins writhe underneath this pulsating white mass, connecting it to an ever growing network of Heads.")]
public class HydraHeart : AIActor, IBaseActionModifier {
  public static int spawnRange = 4;
  public static bool IsTarget(Body b) {
    if (b is Player p && p.isCamouflaged) {
      return false;
    }
    return !(b is HydraHead) && !(b is HydraHeart);
  }
  private List<HydraHead> heads = new List<HydraHead>();
  public HydraHeart(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 9;
    // inventory.AddItem(new ItemHydraPearl());
  }

  private bool needsWait = true;
  protected override ActorTask GetNextTask() {
    if (needsWait) {
      needsWait = false;
      return new WaitTask(this, 2);
    } else {
      // update head count
      heads.RemoveAll(h => h.IsDead);
      if (heads.Count() < 12) {
        needsWait = true;
        return new TelegraphedTask(this, 1, new GenericBaseAction(this, SpawnHydraHead));
      } else {
        return new WaitTask(this, 1);
      }
    }
  }

  void SpawnHydraHead() {
    Tile spawnTile = null;

    var nearestEnemy = floor.ActorsInCircle(pos, spawnRange).Where(IsTarget).OrderBy((a) => a.DistanceTo(pos)).FirstOrDefault();
    if (nearestEnemy != null) {
      spawnTile = Util.RandomPick(floor.GetAdjacentTiles(nearestEnemy.pos).Where(CanSpawnHydraHeadAt));
    }

    if (spawnTile == null) {
      spawnTile = Util.RandomPick(floor.EnumerateCircle(base.pos, spawnRange).Select(p => floor.tiles[p]).Where(CanSpawnHydraHeadAt));
    }

    if (spawnTile != null) {
      var head = new HydraHead(spawnTile.pos);
      head.ClearTasks();
      if (this.faction == Faction.Ally) {
        head.SetAI(new CharmAI(head));
      }
      floor.Put(head);
      heads.Add(head);
    }
  }

  // make sure the hydra heart can see the tile
  private bool CanSpawnHydraHeadAt(Tile t) => t.CanBeOccupied() && floor.TestVisibility(pos, t.pos);

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    GameModel.main.EnqueueEvent(() => {
      foreach (var head in heads) {
        head.Kill(source);
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

[Serializable]
[ObjectInfo("hydra-pearl")]
public class ItemHydraPearl : Item, IEdible {
  public void Eat(Actor a) {
    a.Heal(2);
    a.statuses.Add(new ConfusedStatus(5));
    Destroy();
  }

  internal override string GetStats() => "Eat to heal 2 HP and become confused for 5 turns.";
}

[System.Serializable]
[ObjectInfo(description: "Attacks anything adjacent to it.\nStationary.", flavorText: "A fleshy tube with a gaping jaw at the end, grasping at any food nearby.")]
public class HydraHead : AIActor, IBaseActionModifier, INoTurnDelay {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.ATTACK] = 2f,
  };

  protected override ActionCosts actionCosts => StaticActionCosts;
  public HydraHead(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 3;
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);

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