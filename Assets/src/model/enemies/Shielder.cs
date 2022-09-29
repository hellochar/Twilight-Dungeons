using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class Shielder : AIActor {
  public Shielder(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 1;
  }

  ShieldLinkStatus status;

  protected override ActorTask GetNextTask() {
    if (status == null) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, LinkWithClosestTarget));
    }
    return new WaitTask(this, 1);
  }

  public override void HandleDeath(Entity source) {
    if (status != null) {
      status.Remove();
    }
    base.HandleDeath(source);
  }

  void LinkWithClosestTarget() {
    var closestTarget = floor.bodies.Where(b => b is Actor a && !(b is Shielder)).Cast<Actor>().OrderBy(DistanceTo).FirstOrDefault();
    if (closestTarget != null) {
      status = new ShieldLinkStatus(this);
      closestTarget.statuses.Add(status);
    }
  }

  internal void TargetDied() {
    status = null;
  }
}

[Serializable]
public class ShieldLinkStatus : Status, IAnyDamageTakenModifier, IDeathHandler {
  public Shielder shielder;

  public ShieldLinkStatus(Shielder shielder) {
    this.shielder = shielder;
  }

  public override bool Consume(Status other) { return false; }

  public void HandleDeath(Entity source) {
    shielder.TargetDied();
  }

  public override void Step() {
    if (shielder.floor.TestVisibility(shielder.pos, actor.pos) != TileVisiblity.Visible) {
      Remove();
    }
  }

  public override string Info() => "The shielder will block 1 damage from all sources.";

  public int Modify(int input) {
    if (shielder.IsDead) {
      // schedule
      Remove();
      return input;
    }
    return input - 1;
  }
}