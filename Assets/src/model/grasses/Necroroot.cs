using System;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Spawn a Zombie of any creature that dies on the Necroroot (this consumes the Necroroot).\nZombies attack anything nearby and lose 1 HP while not standing on Necroroot.")]
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

  public float ageCorpseCaptured { get; private set; }
  private void HandleBodyDied() {
    var actor = this.actor;
    if (corpse == null && actor != null && !(actor is Zombie)) {
      ageCorpseCaptured = age;
      corpse = actor;
      AddTimedEvent(3.01f, CreateZombie);
    }
  }

  private void CreateZombie() {
    floor.Put(new Zombie(pos, corpse));
    KillSelf();
  }
}

[Serializable]
[ObjectInfo(description: "Attacks anything nearby and loses 1 HP per turn not standing on Necroroot.")]
public class Zombie : AIActor, IActionPerformedHandler {
  public override string displayName => $"Zombie {baseActor.displayName}";
  public readonly Actor baseActor;
  public Zombie(Vector2Int pos, Actor baseActor) : base(pos) {
    this.baseActor = baseActor;
    hp = baseMaxHp = baseActor.maxHp;
    faction = Faction.Enemy;
    ClearTasks();
  }

  public override string description => base.description;

  internal override (int, int) BaseAttackDamage() {
    return baseActor.BaseAttackDamage();
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (!(grass is Necroroot)) {
      this.TakeDamage(1, this);
    }
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
