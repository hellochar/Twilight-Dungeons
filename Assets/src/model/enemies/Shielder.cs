using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("If the floor is cleared and there are over 12 shielders on the map, ")]
public class Shielder : AIActor, IDeathHandler {
  public static Item HomeItem => new ItemShielderSpore();
  public Shielder(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 1;
    ClearTasks();
  }

  ShieldLinkStatus status;

  public bool bDuplicated = false;
  private void Duplicate() {
    bDuplicated = true;
    var freeAdjacentSpot = floor.GetAdjacentTiles(pos).Where(t => t.CanBeOccupied()).OrderBy(this.DistanceTo).FirstOrDefault();
    if (freeAdjacentSpot != null) {
      floor.Put(new Shielder(freeAdjacentSpot.pos));
    } else {
      this.statuses.Add(new DeathlyStatus());
    }
  }

  protected override ActorTask GetNextTask() {
    if (bDuplicated) {
      return new WaitTask(this, 9999);
    }
    return new TelegraphedTask(this, 1, new GenericBaseAction(this, Duplicate));
    // if (floor.EnemiesLeft() == 0) {
    //   return new WaitTask(this, 1);
    // }
    // if (status == null) {
    //   return new TelegraphedTask(this, 1, new GenericBaseAction(this, LinkWithClosestTarget));
    // } else {
    //   return new GenericTask(this, MaintainLink);
    // }
    // return new WaitTask(this, 1);
  }

  public override void HandleDeath(Entity source) {
    if (status != null) {
      status.Remove();
    }
    foreach(var actor in floor.AdjacentBodies(pos).OfType<Actor>()) {
      actor.statuses.Add(new EaterStatus());
    }
    base.HandleDeath(source);
  }

  void LinkWithClosestTarget() {
    var closestTarget = floor.bodies.Where(b =>
      b is Actor a && !(b is Shielder) && floor.TestVisibility(pos, b.pos)
    ).Cast<Actor>().OrderBy(DistanceTo).FirstOrDefault();
    if (closestTarget != null) {
      status = new ShieldLinkStatus(this);
      closestTarget.statuses.Add(status);
    }
  }

  void MaintainLink() {
    // if visibility is lost, break status
    var seesActor = floor.TestVisibility(pos, status.actor.pos);
    if (!seesActor || status.actor.IsDead) {
      status.Remove();
    }
  }

  internal void StatusLost() {
    status = null;
  }
}

internal class DeathlyStatus : Status {
    public DeathlyStatus() {
    }

    public override bool Consume(Status other) {
      return true;
    }

    public override string Info() {
      return "Kill...";
    }

    public override void Step() {
      // spread to to other Shielders
      foreach (var shielder in actor.floor.AdjacentActors(actor.pos).OfType<Shielder>()) {
        shielder.statuses.Add(new DeathlyStatus());
      }
      actor.TakeDamage(1, actor);
    }
}

[Serializable]
[ObjectInfo("shielder")]
internal class EaterStatus : Status {
  public override string Info() => "Out pops a Shielder at a random time. It will give you an Armored status.";
  public EaterStatus() {
  }

  public override bool Consume(Status other) {
    return false;
  }

  public override void Step() {
    if (MyRandom.value < 0.05f) {
      actor.statuses.Add(new ArmoredStatus());
      actor.floor.Put(new Shielder(actor.pos));
      Remove();
    }
  }
}

[Serializable]
[ObjectInfo("shielder", description: "Use to spawn a Shielder next to you.")]
internal class ItemShielderSpore : EquippableItem, ITargetedAction<Tile> {
  public override EquipmentSlot slot => EquipmentSlot.Offhand;

  string ITargetedAction<Tile>.TargettedActionName => "Spawn";
  string ITargetedAction<Tile>.TargettedActionDescription => "Choose where to spawn the Shielder.";
  void ITargetedAction<Tile>.PerformTargettedAction(Player player, Entity target) {
    player.floor.Put(new Shielder(target.pos));
    Destroy();
  }

  IEnumerable<Tile> ITargetedAction<Tile>.Targets(Player player) {
    return player.floor.GetAdjacentTiles(player.pos).Where(t => t.CanBeOccupied());
  }
}

[Serializable]
[ObjectInfo("shielder")]
public class ShieldLinkStatus : Status, IAnyDamageTakenModifier {
  public Shielder shielder;

  public ShieldLinkStatus(Shielder shielder) {
    this.shielder = shielder;
  }

  public override bool Consume(Status other) { return false; }

  public override void End() {
    base.End();
    shielder.StatusLost();
  }

  public override void Step() {
    if (shielder.floor != actor.floor || !shielder.floor.TestVisibility(shielder.pos, actor.pos)) {
      Remove();
    }
  }

  public override string Info() => "The Shielder has linked you! Block 1 damage from all sources.";

  public int Modify(int input) {
    if (shielder.IsDead) {
      // schedule
      Remove();
      return input;
    }
    if (input > 0) {
      shielder.TakeDamage(1, actor);
    }
    return input - 1;
  }

  public override void HandleFloorChanged(Floor newFloor, Floor oldFloor) {
    Remove();
  }
}