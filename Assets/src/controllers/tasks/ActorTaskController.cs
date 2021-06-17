using System;
using UnityEngine;

/**
 * Create - created when prefab is Instantiated
 * Destroy - self handled
 */
public class ActorTaskController : MonoBehaviour {
  [NonSerialized]
  public Actor actor;
  [NonSerialized]
  public ActorTask task;

  public virtual void Update() {
    if (actor.task != task) {
      var anim = gameObject.AddComponent<FadeThenDestroy>();
      anim.fadeTime = 0.25f;
      anim.shrink = 0f;
      if (task is SleepTask) {
        Destroy(this.gameObject);
      }
    }
  }
}
