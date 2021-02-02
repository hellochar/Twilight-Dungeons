using System.Collections.Generic;

[System.Serializable]
public class AttackTask : DoOnceTask {
  public AttackTask(Actor actor, Body target) : base(actor) {
    this.target = target;
  }

  public Body target { get; }

  protected override BaseAction GetNextActionImpl() {
    return new AttackBaseAction(actor, target);
  }
}
