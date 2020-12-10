using System;
using UnityEngine;

public class ActorTaskController<T> : MonoBehaviour where T : ActorTask {
  public Actor actor;
  public T action;

  public virtual void Start() {
    actor = GetComponentInParent<ActorController>().actor;
    try {
      action = (T) actor.task;
    } catch (InvalidCastException) {
      Destroy(this.gameObject);
    }
  }

  public virtual void Update() {
    if (actor.task != action) {
      Destroy(this.gameObject);
    }
  }
}

public class MatchActorTask : ActorTaskController<ActorTask> {}
