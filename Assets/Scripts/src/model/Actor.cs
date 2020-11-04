using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actor : Entity {
  private Vector2Int _pos;
  public virtual Vector2Int pos {
    get => _pos;
    set {
      Floor floor = this.floor;
      if (floor == null || floor.tiles[value.x, value.y].CanBeOccupied()) {
        _pos = value;
      }
    }
  }
  public int baseActionCost { get => 1; }
  public int timeCreated { get; }
  /// how many turns this Entity has been alive for
  public int age { get => GameModel.main.time - timeCreated; }
  public int timeNextAction;
  public virtual ActorAction action { get; set; }
  public int visibilityRange = 7;
  public Floor floor;
  public Tile currentTile => floor.tiles[pos.x, pos.y];
  public bool visible => currentTile.visiblity == TileVisiblity.Visible;

  /// This number allows tweaking of Actor order when they would otherwise be scheduled
  /// at the same time. This offset gets added to the timeNextAction, so higher numbers
  /// will come after lower numbers. This does *NOT* actually modify "time" which the
  /// actor takes the action. Player has offset 0 (aka always goes first). This number should
  /// be < 1.
  internal virtual float queueOrderOffset { get => 0.5f; }

  public Actor(Vector2Int pos) {
    this.timeCreated = GameModel.main.time;
    this.timeNextAction = this.timeCreated;
    this.pos = pos;
  }

  public virtual void Step() {
    if (action == null) {
      this.timeNextAction += baseActionCost;
    } else {
      if (action.IsDone()) {
        this.action = null;
        this.timeNextAction += baseActionCost;
      } else {
        int timeCost = action.Perform();
        this.timeNextAction += timeCost;
        if (action.IsDone()) {
          this.action = null;
        }
      }
    }
  }

  public virtual void CatchUpStep(int newTime) {
    // by default actors don't do anything; they just act as if they were paused
    this.timeNextAction = newTime;
  }
}

public class BerryBush : Actor {
  class BerrySeed : PlantStage<BerryBush> {
    public BerrySeed(BerryBush plant) : base(plant) {}
    public override void Step() {
      if (this.age >= 5) {
        plant.currentStage = new YoungBerryBush(plant);
      }
    }
  }

  class YoungBerryBush : PlantStage<BerryBush> {
    public YoungBerryBush(BerryBush plant) : base(plant) {}
    public override void Step() {
      if (this.age >= 5) {
        plant.currentStage = new MatureBerryBush(plant);
      }
    }
  }

  class MatureBerryBush : PlantStage<BerryBush> {
    public int numBerries = 0;
    public MatureBerryBush(BerryBush plant) : base(plant) {}
    public override void Step() {
    }
  }

  public PlantStage<BerryBush> currentStage;

  internal override float queueOrderOffset => 0.4f;

  public BerryBush(Vector2Int pos) : base(pos) {
    currentStage = new BerrySeed(this);
  }

  public override void Step() {
    this.currentStage.Step();
    this.timeNextAction = GameModel.main.time + baseActionCost;
  }

  public override void CatchUpStep(int time) {
    Debug.Log("catching up " + this + " from " + this.timeNextAction + " to " + time);
    while (this.timeNextAction < time) {
      this.Step();
    }
  }
}

public class PlantStage<T> where T : Actor {
  public T plant;
  public int ageEntered { get; }
  /// how long the plant has been in this stage specifically
  public int age { get => plant.age - ageEntered; }
  public string name { get => GetType().Name; }

  public PlantStage(T plant) {
    this.plant = plant;
    this.ageEntered = plant.age;
  }

  public virtual void Step() {}
}

public class Bat : Actor {
  class BatAIAction : ActorAction {
    internal BatAIAction(Bat bat) : base(bat) {}

    public override int Perform() {
      // randomly move 
      Vector2Int dir = (new Vector2Int[] {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,
      })[UnityEngine.Random.Range(0, 4)];
      actor.pos += dir;
      return actor.baseActionCost;
    }

    public override bool IsDone() {
      return false;
    }
  }

  public Bat(Vector2Int pos) : base(pos) {
    this.action = new BatAIAction(this);
  }
}

public abstract class ActorAction {
  public virtual Actor actor { get; }

  protected ActorAction(Actor actor) { this.actor = actor; }

  /// return the number of ticks it took to perform this action
  public abstract int Perform();

  public virtual bool IsDone() {
    return true;
  }
}

public class TeleportAction : ActorAction {
  Vector2Int target;
  public TeleportAction(Actor actor, Vector2Int target) : base(actor) {
    this.target = target;
  }

  public override int Perform() {
    actor.pos = target;
    return actor.baseActionCost;
  }
}

public class MoveToTargetAction : ActorAction {
  public Vector2Int target { get; }
  public readonly List<Vector2Int> path;

  public MoveToTargetAction(Actor actor, Vector2Int target) : base(actor) {
    this.target = target;
    Floor floor = GameModel.main.currentFloor;
    this.path = floor.FindPath(actor.pos, target);
  }

  public override int Perform() {
    if (path.Count > 0) {
      Vector2Int nextPosition = path[0];
      path.RemoveAt(0);
      actor.pos = nextPosition;
    }
    return actor.baseActionCost;
  }

  public override bool IsDone() {
    return path.Count == 0;
    // return actor.pos == target;
  }
}

// public class MoveAction {
//   public int Perform(Entity entity) {
//     return entity.baseActionCost;
//   }
// }
