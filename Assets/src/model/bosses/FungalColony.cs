using System;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo("fungal-colony", description: "Blocks 4 attack damage.\nSummons a Fungal Breeder every 12 turns.\nDoes not move or attack.")]
public class FungalColony : Boss, IAttackDamageTakenModifier {
  public FungalColony(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 72;
    faction = Faction.Enemy;
  }

  bool needsWait = false;
  protected override ActorTask GetNextTask() {
    if (needsWait) {
      needsWait = false;
      return new WaitTask(this, 11);
    } else {
      // return new TelegraphedTask(this, 1, new GenericBaseAction(this, SummonFungalWalls));
      return new GenericTask(this, SummonFungalBreeder);
    }
  }

  private bool shouldSummonClose = true;
  void SummonFungalBreeder() {
    Vector2Int? newPos;
    if (shouldSummonClose) {
      newPos = Util.RandomPick(floor.GetAdjacentTiles(GameModel.main.player.pos).Where(t => t.CanBeOccupied()))?.pos;
      shouldSummonClose = false;
    } else {
      newPos = Util.RandomPick(floor.EnumerateCircle(pos, 5f).Where(p => floor.tiles[p].CanBeOccupied() && DistanceTo(p) >= 4f));
      shouldSummonClose = true;
    }

    if (newPos.HasValue) {
      needsWait = true;
      floor.Put(new FungalBreeder(newPos.Value));
    }
  }

  public int Modify(int input) {
    return input - 4;
  }
}

[Serializable]
[ObjectInfo("fungal-breeder", description: "Summons a Fungal Sentinel every 7 turns.\nDoes not move or attack.")]
public class FungalBreeder : AIActor {
  public FungalBreeder(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 10;
    faction = Faction.Enemy;
    ClearTasks();
    // SetTasks(new WaitTask(this, MyRandom.Range(1, 4)));
  }

  bool needsWait = false;
  protected override ActorTask GetNextTask() {
    if (needsWait) {
      needsWait = false;
      return new WaitTask(this, 6);
    } else {
      needsWait = true;
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, SummonFungalBloat));
    }
  }

  void SummonFungalBloat() {
    var tile = Util.RandomPick(floor.GetAdjacentTiles(pos).Where(t => t.CanBeOccupied()));
    if (tile != null) {
      floor.Put(new FungalSentinel(tile.pos));
    }
  }
}

[System.Serializable]
[ObjectInfo("fungal-wall", description: "Blocks vision and movement.\nWalk into to remove.")]
public class FungalWall : Wall {
  public float timeNextAction { get; set; }

  public float turnPriority => 90;

  public FungalWall(Vector2Int pos) : base(pos) {
    // timeNextAction = GameModel.main.time + MyRandom.Range(35, 60);
    timeNextAction = GameModel.main.time + 10;
  }

  public float Step() {
    var floor = this.floor;
    GameModel.main.EnqueueEvent(() => floor.Put(new FungalSentinel(pos)));
    floor.Put(new Ground(pos));
    return 50;
  }

  internal void Clear() {
    // SummonGrowingFungi(); 
    floor.Put(new Ground(pos));
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    var floor = this.floor;
    if (GameModel.main.player.floor == floor) {
      GameModel.main.EnqueueEvent(() => floor.RecomputeVisiblity(GameModel.main.player));
    }
  }

  protected override void HandleLeaveFloor() {
    base.HandleLeaveFloor();
    var floor = this.floor;
    GameModel.main.EnqueueEvent(() => floor.RecomputeVisiblity(GameModel.main.player));
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
    timeNextAction += 1;
    statuses.Add(new SurprisedStatus());
  }

  public void Explode() {
    foreach (var actor in floor.AdjacentActors(pos).Where(actor => actor != this)) {
      actor.TakeDamage(2, this);
      // var numCoughStatus = actor.statuses.FindOfType<WheezingStatus>()?.stacks ?? 0;
      // Attack(actor, GetFinalAttackDamage() + numCoughStatus);
      actor.statuses.Add(new WheezingStatus());
    }
    floor.Put(new FungalWall(pos));
    // floor.Put(new SentinelEgg(pos));
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
      // var tiles = floor 
      //   .GetAdjacentTiles(pos)
      //   .Where(t => t.CanBeOccupied())
      //   .OrderByDescending((t) => floor.GetAdjacentTiles(t.pos).Where(t2 => t2.CanBeOccupied()).Count());
      // var tile = tiles.FirstOrDefault();

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

      // return new ChaseTargetTask(this, GameModel.main.player);
    }
  }

  public void HandleTakeAnyDamage(int damage) {
    if (!shouldExplode) {
      shouldExplode = true;
      SetTasks(new ExplodeTask(this));
    }
  }
}

[ObjectInfo("fungal-sentinel")]
[Serializable]
internal class WheezingStatus : StackingStatus {
  public WheezingStatus() : base(1) { }

  int turnCounter = 10;
  public override void Step() {
    turnCounter--;
    if (turnCounter <= 0) {
      turnCounter = 10;
      stacks--;
    }
  }

  public override string Info() => $"Take {stacks} more damage from Fungal Sentinels. Lose a stack every 10 turns.";
}