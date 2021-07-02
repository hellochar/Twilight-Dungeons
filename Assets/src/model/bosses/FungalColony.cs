using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo("fungal-colony", description: "Spawns Growing Fungus around it. Once you kill it, you must kill all Growing Fungi within 30 turns or the Colony will grow back with 50% HP.")]
public class FungalColony : Boss {
  public FungalColony(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 60;
    faction = Faction.Enemy;
  }

  protected override void OnSeen() {
    foreach(var spawner in floor.bodies.Where(b => b is FungalBreeder).Cast<FungalBreeder>()) {
      spawner.ClearTasks();
      spawner.SetTasks(new WaitTask(spawner, MyRandom.Range(1, 5)));
    }
    base.OnSeen();
  }

  bool needsWait = false;
  protected override ActorTask GetNextTask() {
    if (needsWait) {
      needsWait = false;
      return new WaitTask(this, 3);
    } else {
      needsWait = true;
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, PlantSentinelEggs));
    }
  }

  void PlantSentinelEggs() {
    var positions = floor
      .BreadthFirstSearch(pos)
      .Where(t => SentinelEgg.CanOccupy(t));

    foreach (var tile in positions.Take(5)) {
      floor.Put(new SentinelEgg(tile.pos));
    }
  }
}

[System.Serializable]
public class FungalBreeder : AIActor {
  public FungalBreeder(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 2;
    faction = Faction.Enemy;
    // SetTasks(new WaitTask(this, MyRandom.Range(1, 4)));
  }

  bool needsWait = false;
  protected override ActorTask GetNextTask() {
    // if (needsWait) {
    //   needsWait = false;
    //   return new WaitTask(this, 1);
    // } else {
    //   needsWait = true;
    return new TelegraphedTask(this, 1, new GenericBaseAction(this, SummonFungalBloat));
    // }
  }

  void SummonFungalBloat() {
    floor.Put(new FungalSentinel(pos));
  }

  void PlantSentinelEggs() {
    var positions = floor
      .BreadthFirstSearch(pos)
      .Where(t => SentinelEgg.CanOccupy(t));

    foreach (var tile in positions.Take(1)) {
      floor.Put(new SentinelEgg(tile.pos));
    }
  }
}

[System.Serializable]
public class SentinelEgg : Grass, IActorEnterHandler, ISteppable, INoTurnDelay {
  public static bool CanOccupy(Tile tile) {
    var floor = tile.floor;
    var isGround = tile is Ground;
    var isNotOccupied = tile.CanBeOccupied() && !(tile.grass is SentinelEgg);
    
    return isGround && isNotOccupied;
  }

  public SentinelEgg(Vector2Int pos) : base(pos) {
    timeNextAction = GameModel.main.time + 25;
  }
  public float timeNextAction { get; set; }

  public float turnPriority => 51;

  public float Step() {
    floor.Put(new FungalSentinel(pos));
    KillSelf();
    return 50;
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player) {
    //   floor.Put(new FungalSentinel(pos));
      Kill(who);
    }
  }
}

[System.Serializable]
public class FungalWall : Wall {
  public FungalWall(Vector2Int pos) : base(pos) { }

  internal void Clear() {
    // SummonGrowingFungi(); 
    floor.Put(new Ground(pos));
  }

  // protected override void HandleEnterFloor() {
  //   base.HandleEnterFloor();
  //   var floor = this.floor;
  //   GameModel.main.EnqueueEvent(() => floor.RecomputeVisiblity(GameModel.main.player));
  // }

  protected override void HandleLeaveFloor() {
    base.HandleLeaveFloor();
    var floor = this.floor;
    GameModel.main.EnqueueEvent(() => floor.RecomputeVisiblity(GameModel.main.player));
  }
}

[System.Serializable]
public class FungalSentinel : AIActor, ITakeAnyDamageHandler, IDeathHandler, INoTurnDelay {
  public override float turnPriority => task is TelegraphedTask ? 49 : 50;
  public FungalSentinel(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 2;
    faction = Faction.Enemy;
    ClearTasks();
    timeNextAction += 1;
    statuses.Add(new SurprisedStatus());
  }

  private bool shouldExplode = false;

  protected override ActorTask GetNextTask() {
    if (shouldExplode) {
      return new GenericTask(this, KillSelf);
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
        .Where(t => t.CanBeOccupied())
        .OrderBy((t) => t.DistanceTo(GameModel.main.player)).FirstOrDefault();

      if (tile != null) {
        return new MoveToTargetTask(this, tile.pos);
      } else {
        return new MoveRandomlyTask(this);
      }

      // return new ChaseTargetTask(this, GameModel.main.player);
    }
  }

  public override void HandleDeath(Entity source) {
    foreach (var actor in floor.AdjacentActors(pos).Where(actor => !(actor is FungalColony))) {
      actor.TakeAttackDamage(1, this);
    }
    // floor.Put(new SentinelEgg(pos));
    base.HandleDeath(source);
  }

  public void HandleTakeAnyDamage(int damage) {
    if (!shouldExplode) {
      shouldExplode = true;
      SetTasks(new ExplodeTask(this));
    }
  }
}