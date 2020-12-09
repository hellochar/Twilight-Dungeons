using System;
using UnityEngine;

public class MatchActorTask<T> : MonoBehaviour where T : ActorTask {
  public Actor actor;
  public T action;

  public virtual void Start() {
    actor = GetComponentInParent<MatchActorState>().actor;
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

public class MatchActorTask : MatchActorTask<ActorTask> {}
