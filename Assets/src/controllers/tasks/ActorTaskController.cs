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
  protected virtual bool removeImmediately => false;
  private FadeThenDestroy anim;

  public virtual void Update() {
    if (actor.task != task) {
      if (removeImmediately) {
        Destroy(this.gameObject);
      } else if (anim == null) {
        anim = gameObject.AddComponent<FadeThenDestroy>();
        anim.fadeTime = 0.25f;
        anim.shrink = 0f;
      }
    }
  }
}
