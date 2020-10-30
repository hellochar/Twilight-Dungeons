using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity {
  public virtual Vector2Int pos { get; set; }
  public int age = 0;
  public int nextActionTime = 0;
  public List<Action> actions;
  public int visibilityRange = 7;

  public Entity(Vector2Int pos) {
    this.pos = pos;
    this.actions = new List<Action>();
  }

  public void Step() {
    Action action = this.actions[0];
    if (action == null) {
      action = Action.DoNothing;
    }
    this.age += action.Perform(this);
    this.actions.Remove(action);
  }

  public int baseActionCost {
    get {
      return 100;
    }
  }
}

public abstract class Action {
  public static DoNothingAction DoNothing = new DoNothingAction();

  /// return the number of ticks it took to perform this action
  public abstract int Perform(Entity entity);

  public virtual bool IsDone() {
    return true;
  }
}

public class DoNothingAction : Action {
  public override int Perform(Entity entity) {
    return entity.baseActionCost;
  }
}

public class TeleportAction : Action {
  Vector2Int target;
  public TeleportAction(Vector2Int target) {
    this.target = target;
  }

  public override int Perform(Entity entity) {
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
