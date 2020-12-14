using System;
using UnityEngine;

/**
 * Create - created when prefab is Instantiated
 * Destroy - self handled
 */
public class ActorTaskController : MonoBehaviour {
  public Actor actor;
  public ActorTask task;

  public virtual void Start() {
  //   Debug.LogError(this + " expecting actor's task to be of type " + typeof(T) + " but got " + actor.task.GetType() + " instead!");
  //   Destroy(this.gameObject);
  }

  public virtual void Update() {
    if (actor.task != task) {
      Destroy(this.gameObject);
    }
  }
}
