using System;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "When a creature dies on the Necroroot, spawn a Zombie of that creature after 3 turns.\nZombies attack anything nearby and lose 1 HP per turn. When it dies, it leaves a new Necroroot.")]
public class Necroroot : Grass {
  public Actor corpse;

  [Serializable]
  private class NecrorootBodyModifier : IDeathHandler {
    private Necroroot owner;

    public NecrorootBodyModifier(Necroroot owner) {
      this.owner = owner;
    }

    public void HandleDeath(Entity source) {
      owner.HandleBodyDied();
    }
  }

  public Necroroot(Vector2Int pos) : base(pos) {
    BodyModifier = new NecrorootBodyModifier(this);
  }

  private void HandleBodyDied() {
    var actor = this.actor;
    if (corpse == null && actor != null) {
      corpse = actor;
      AddTimedEvent(3, CreateZombie);
    }
  }

  private void CreateZombie() {
    floor.Put(new Zombie(pos, corpse));
    KillSelf();
  }
}

[Serializable]
[ObjectInfo("Has the same HP and damage of the base creature.\nAttacks anything nearby and loses 1 HP per turn.\nLeaves a Necroroot when it dies.")]
public class Zombie : AIActor, IActionPerformedHandler {
  public override string displayName => $"Zombie {baseActor.displayName}";
  public readonly Actor baseActor;
  public Zombie(Vector2Int pos, Actor baseActor) : base(pos) {
    this.baseActor = baseActor;
    hp = baseMaxHp = baseActor.maxHp;
    faction = Faction.Enemy;
    ClearTasks();
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    floor.Put(new Necroroot(pos));
  }

  internal override (int, int) BaseAttackDamage() {
    return baseActor.BaseAttackDamage();
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    this.TakeDamage(1, this);
  }

  protected override ActorTask GetNextTask() {
    var target = SelectTarget();
    if (target == null) {
      return new MoveRandomlyTask(this);
    }
    if (IsNextTo(target)) {
      return new AttackTask(this, target);
    }
    // chase until you are next to any target
    return new ChaseDynamicTargetTask(this, SelectTarget);
  }

  Body SelectTarget() {
    var potentialTargets = floor
      .BodiesInCircle(pos, 7)
      .Where((t) => floor.TestVisibility(pos, t.pos) && !(t is Zombie));
    if (potentialTargets.Any()) {
      return potentialTargets.Aggregate((t1, t2) => DistanceTo(t1) < DistanceTo(t2) ? t1 : t2);
    }
    return null;
  }
}
