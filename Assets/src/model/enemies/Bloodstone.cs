using System;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo("bloodstone", flavorText: "", description: "You deal +1 attack damage.\nYou take +1 attack damage.\n\nDestroy the Bloodstone to remove.")]
public class Bloodstone : Body {
  public Bloodstone(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    if (floor == GameModel.main.player?.floor) {
      GameModel.main.player.statuses.Add(new BloodstoneStatus());
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
    var status = GameModel.main.player.statuses.FindOfType<BloodstoneStatus>();
    if (status != null) {
      status.Refresh();
    }
  }

  private void HandleEntityAdded(Entity entity) {
    if (entity is Player p) {
      p.statuses.Add(new BloodstoneStatus());
    }
  }
}

[Serializable]
[ObjectInfo("bloodstone")]
public class BloodstoneStatus : StackingStatus, IAttackDamageModifier, IAttackDamageTakenModifier, IFloorChangeHandler {
  public BloodstoneStatus() {}

  public override bool Consume(Status otherParam) {
    Refresh();
    return true;
  }

  public void Refresh() {
    var numBloodstones = actor.floor.bodies.Where(b => b is Bloodstone).Count();
    stacks = numBloodstones;
  }

  public override void HandleFloorChanged(Floor newFloor, Floor oldFloor) {
    if (newFloor != null) {
      Refresh();
    }
  }

  public override string Info() {
    return $"Deal and take +{stacks} damage.";
  }

  // this is super hacky but this implementation is executed for both IAttackDamage and IAttackDamageTaken
  public int Modify(int input) {
    return input + stacks;
  }
}