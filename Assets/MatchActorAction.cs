using System;
using UnityEngine;

public class MatchActorAction<T> : MonoBehaviour where T : ActorAction {
  public Actor actor;
  public T action;

  public virtual void Start() {
    actor = GetComponentInParent<MatchActorState>().actor;
    try {
      action = (T) actor.action;
    } catch (InvalidCastException) {
      Destroy(this.gameObject);
    }
  }

  public virtual void Update() {
    if (actor.action != action) {
      Destroy(this.gameObject);
    }
  }
}
