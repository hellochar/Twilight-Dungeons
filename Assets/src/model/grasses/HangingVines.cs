using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Constricts any creature that walks into its hook.\nYou may destroy the Hanging Vines by tapping the stem.")]
public class HangingVines : Grass, IDeathHandler {
  private Inventory inventory = new Inventory(new ItemVineWhip(1));
  public Tile tileBelow => floor.tiles[pos + new Vector2Int(0, -1)];
  private HangingVinesTrigger triggerBelow;

  public HangingVines(Vector2Int pos) : base(pos) {
    triggerBelow = new HangingVinesTrigger(this);
  }

  protected override void HandleEnterFloor() {
    floor.Put(triggerBelow);
  }

  protected override void HandleLeaveFloor() {
    floor.Remove(triggerBelow);
  }

  public void HandleDeath(Entity source) {
    inventory.TryDropAllItems(floor, tileBelow.pos);
    if (appliedStatus != null) {
      appliedStatus.Remove();
    }
  }

  private ConstrictedStatus appliedStatus;
  public void HandleActorEnterBelow(Actor who) {
    appliedStatus = new ConstrictedStatus(this);
    who.statuses.Add(appliedStatus);
    OnNoteworthyAction();
  }

  public void ConstrictedStatusEnded() {
    // when someone is able to break free; remove these vines
    var actor = appliedStatus.actor;
    appliedStatus = null;
    Kill(actor);
  }

  internal void ConstrictedCreatureDied() {
    // simply unset the reference
    appliedStatus = null;
  }
}

[Serializable]
public abstract class Trigger : Entity {}

[Serializable]
class HangingVinesTrigger : Trigger, IActorEnterHandler {
  HangingVines owner;
  public override Vector2Int pos {
    get => owner.tileBelow.pos;
    set { }
  }

  public HangingVinesTrigger(HangingVines owner) {
    this.owner = owner;
  }

  public void HandleActorEnter(Actor obj) {
    owner.HandleActorEnterBelow(obj);
  }
}

[Serializable]
internal class ItemVineWhip : EquippableItem, IWeapon, IAttackHandler, IStackable {
  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }

  public int stacksMax => 7;
  public (int, int) AttackSpread => (stacks, stacks);
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  public ItemVineWhip(int stacks) {
    this.stacks = stacks;
  }

  public void OnAttack(int damage, Body target) {
    if (!(target is Rubble)) {
      Destroy();
    }
  }

  internal override string GetStats() => "Damage is equal to number of stacks.\nDestroyed on use.";
}

[System.Serializable]
[ObjectInfo("constricted-status", flavorText: "Thick, damp vines tighten around you!")]
public class ConstrictedStatus : StackingStatus, IBaseActionModifier, IBodyMoveHandler, IDeathHandler {
  private readonly HangingVines owner;

  public override bool isDebuff => true;
  public override StackingMode stackingMode => StackingMode.Max;
  public ConstrictedStatus(HangingVines owner) {
    stacks = 3;
    this.owner = owner;
  }

  public override void End() {
    owner.ConstrictedStatusEnded();
    base.End();
  }

  public void HandleDeath(Entity source) {
    // signal back to the hanging vines that the creature died with this status on it.
    owner.ConstrictedCreatureDied();
  }

  // if they somehow do move (e.g. forced movement), remove this status
  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    if (newPos != oldPos) {
      Remove();
    }
  }

  public override string Info() => $"You must break free of vines before you can move or attack!\n{stacks} stacks left.";

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.MOVE || input.Type == ActionType.ATTACK) {
      stacks--;
      if (stacks <= 0) {
        return input;
      } else {
        return new StruggleBaseAction(input.actor);
      }
    }
    return input;
  }
}