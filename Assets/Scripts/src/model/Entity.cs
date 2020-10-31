using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity {
  public virtual Vector2Int pos { get; set; }
  /// how many turns this Entity has been alive for
  public int age = 0;
  // public int nextActionTime = 0;
  public List<EntityAction> actions;
  public int visibilityRange = 7;

  public Entity(Vector2Int pos) {
    this.pos = pos;
    this.actions = new List<EntityAction>();
  }

  public virtual void Step() {
    EntityAction action = this.actions[0];
    if (action == null) {
      action = EntityAction.DoNothing;
    }
    this.age += action.Perform(this);
    if (action.IsDone()) {
      this.actions.Remove(action);
    }
  }

  public int baseActionCost {
    get {
      return 1;
    }
  }
}

public class BerryBush : Entity {
  class BerrySeed : PlantStage<BerryBush> {
    public BerrySeed(BerryBush plant) : base(plant) {}
    public override void Step() {
      if (this.age > 5) {
        plant.currentStage = new YoungBerryBush(plant);
      }
    }
  }

  class YoungBerryBush : PlantStage<BerryBush> {
    public YoungBerryBush(BerryBush plant) : base(plant) {}
    public override void Step() {
      if (this.age > 5) {
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

  public BerryBush(Vector2Int pos) : base(pos) {
    currentStage = new BerrySeed(this);
  }

  public override void Step() {
    this.age += this.baseActionCost;
    this.currentStage.Step();
  }
}

public class PlantStage<T> where T : Entity {
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

public abstract class EntityAction {
  public static DoNothingAction DoNothing = new DoNothingAction();

  /// return the number of ticks it took to perform this action
  public abstract int Perform(Entity entity);

  public virtual bool IsDone() {
    return true;
  }
}

public class DoNothingAction : EntityAction {
  public override int Perform(Entity entity) {
    return entity.baseActionCost;
  }
}

public class TeleportAction : EntityAction {
  Vector2Int target;
  public TeleportAction(Vector2Int target) {
    this.target = target;
  }

  public override int Perform(Entity entity) {
    entity.pos = target;
    return entity.baseActionCost;
  }
}

// public class FollowPathAction : Action {
//   public List<Vector2Int> path;
//   public FollowPathAction(List<Vector2Int> path) {
//     this.path = path;
//   }
// }

// public class MoveAction {
//   public int Perform(Entity entity) {
//     return entity.baseActionCost;
//   }
// }
