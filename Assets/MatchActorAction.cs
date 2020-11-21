using UnityEngine;

public class MatchActorAction<T> : MonoBehaviour where T : ActorAction {
  public Actor actor;
  public T action;

  public virtual void Start() {
    actor = GetComponentInParent<MatchActorState>().actor;
    action = (T) actor.action;
  }

  public virtual void Update() {
    if (actor.action != action) {
      Destroy(this.gameObject);
    }
  }
}
