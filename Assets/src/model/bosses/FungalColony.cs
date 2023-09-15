using System;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo("fungal-colony", description: "Blocks 1 attack damage.\nSpawns a Fungal Sentinel when attacked.\nCan be damaged by Fungal Sentinel explosions.\nEvery 13 turns, summons a Fungal Breeder and moves itself to a random Fungal Wall.\nDoes not move or attack.")]
public class FungalColony : Boss, IAttackDamageTakenModifier {
  public FungalColony(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 48;
    faction = Faction.Enemy;
    ClearTasks();
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    // kill all breeders and sentinels
    var blobs = floor.bodies.Where(b => b is FungalBreeder || b is FungalSentinel);
    foreach (var b in blobs.ToArray()) {
      b.Kill(this);
    }
  }

  bool needsWait = true;
  protected override ActorTask GetNextTask() {
    if (needsWait) {
      needsWait = false;
      return new WaitTask(this, 13);
    } else {
      return new GenericTask(this, SummonFungalBreeder);
    }
  }

  void SummonFungalBreeder() {
    var player = GameModel.main.player;
    if (player.IsDead) {
      needsWait = true;
      return;
    }
    var nextTile = Util.RandomPick(floor.tiles.Where(t => t is FungalWall));
    if (nextTile != null) {
      needsWait = true;
      var oldPos = pos;
      // remove the tile so it's occupiable
      floor.Put(new Ground(nextTile.pos));
      pos = nextTile.pos;
      floor.Put(new FungalBreeder(oldPos));
    }
  }

  public int Modify(int input) {
    floor.Put(new FungalSentinel(pos));
    return input - 1;
  }
}

[Serializable]
[ObjectInfo("fungal-breeder", description: "Summons a Fungal Sentinel every 7 turns.\nDoes not move or attack.")]
public class FungalBreeder : AIActor {
  public FungalBreeder(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 8;
    faction = Faction.Enemy;
  }

  bool needsWait = false;
  protected override ActorTask GetNextTask() {
    if (needsWait) {
      needsWait = false;
      return new WaitTask(this, 6);
    } else {
      needsWait = true;
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, SummonFungalSentinel));
    }
  }

  void SummonFungalSentinel() {
    var tile = Util.RandomPick(floor.GetAdjacentTiles(pos).Where(t => t.CanBeOccupied()));
    if (tile != null) {
      var sentinel = new FungalSentinel(tile.pos);
      sentinel.statuses.Add(new SurprisedStatus());
      sentinel.timeNextAction += 1;
      floor.Put(sentinel);
    }
  }
}

[System.Serializable]
[ObjectInfo("fungal-wall", description: "Blocks vision and movement.\nWalk into to remove.")]
public class FungalWall : Wall {
  public FungalWall(Vector2Int pos) : base(pos) { }

  internal void Clear() {
    if (IsNextTo(GameModel.main.player)) {
      floor.Put(new Ground(pos));
    }
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    // remove grasses underneath
    grass?.Kill(this);
    var floor = this.floor;
    if (GameModel.main.player.floor == floor) {
      GameModel.main.EnqueueEvent(() => floor.RecomputeVisibility());
    }
  }

  protected override void HandleLeaveFloor() {
    base.HandleLeaveFloor();
    var floor = this.floor;
    GameModel.main.EnqueueEvent(() => floor.RecomputeVisibility());
  }
}

[System.Serializable]
[ObjectInfo("fungal-sentinel", description: "Chases you, then explodes, dealing 2 damage to adjacent Creatures and leaving a Fungal Wall. This can trigger other Sentinels.\nKilling the Sentinel will prevent its explosion.")]
public class FungalSentinel : AIActor, ITakeAnyDamageHandler, IDeathHandler, INoTurnDelay {
  public override float turnPriority => task is ExplodeTask ? 49 : 50;
  [field:NonSerialized]
  public event Action OnExploded;
  public FungalSentinel(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 3;
    faction = Faction.Enemy;
    ClearTasks();
  }

  public void Explode() {
    foreach (var actor in floor.AdjacentActors(pos).Where(actor => actor != this)) {
      actor.TakeDamage(2, this);
    }
    if (tile is Ground) {
      floor.Put(new FungalWall(pos));
    }
    OnExploded?.Invoke();
    KillSelf();
  }

  private bool shouldExplode = false;

  protected override ActorTask GetNextTask() {
    if (shouldExplode) {
      return new GenericTask(this, Explode);
    }
    if (IsNextTo(GameModel.main.player)) {
      shouldExplode = true;
      return new ExplodeTask(this);
    } else {
      var tile = floor 
        .GetAdjacentTiles(pos)
        .Where(t => t.CanBeOccupied() || t == this.tile)
        .OrderBy((t) => t.DistanceTo(GameModel.main.player)).FirstOrDefault();

      if (tile == this.tile) {
        return new WaitTask(this, 1);
      } else if (tile != null) {
        return new MoveToTargetTask(this, tile.pos);
      } else {
        return new MoveRandomlyTask(this);
      }
    }
  }

  public void HandleTakeAnyDamage(int damage) {
    if (!shouldExplode) {
      shouldExplode = true;
      SetTasks(new ExplodeTask(this));
    }
  }
}
