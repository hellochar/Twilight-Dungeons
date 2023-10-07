using System;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo("bloodstone", flavorText: "", description: "You deal +1 attack damage.\nYou take +1 attack damage.\n\nDestroy the Bloodstone to remove.")]
public class Bloodstone : Body {
  BloodstoneStatus status;

  public Bloodstone(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
    status = new BloodstoneStatus(this);
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    if (floor == GameModel.main.player?.floor) {
      GameModel.main.player.statuses.Add(status);
    }
    RegisterEntityAddedListener();
  }

  [OnDeserialized]
  public void RegisterEntityAddedListener() {
    floor.OnEntityAdded += HandleEntityAdded;
  }

  protected override void HandleLeaveFloor() {
    floor.OnEntityAdded -= HandleEntityAdded;
    base.HandleLeaveFloor();
    status.Refresh();
  }

  private void HandleEntityAdded(Entity entity) {
    if (entity is Player p) {
      p.statuses.Add(status);
    }
  }
}

[Serializable]
[ObjectInfo("bloodstone")]
public class BloodstoneStatus : Status, IAttackDamageModifier, IAttackDamageTakenModifier, IFloorChangeHandler {
  private Bloodstone owner;

  public BloodstoneStatus(Bloodstone owner) {
    this.owner = owner;
  }

  public override bool Consume(Status otherParam) {
    return false;
  }

  public void Refresh() {
    var isOwnerOnDifferentFloor = actor.floor != owner.floor;
    var isOwnerDead = owner.IsDead;
    if (isOwnerOnDifferentFloor || isOwnerDead) {
      Remove();
    }
  }

  public override void HandleFloorChanged(Floor newFloor, Floor oldFloor) {
    if (newFloor != null) {
      Refresh();
    }
  }

  public override string Info() => ObjectInfo.GetDescriptionFor(owner);

  // this is super hacky but this implementation is executed for both IAttackDamage and IAttackDamageTaken
  public int Modify(int input) {
    return input + 1;
  }
}