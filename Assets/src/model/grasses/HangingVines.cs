using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Constricts any creature that walks into its hook.\nYou may destroy the Hanging Vines by tapping the Wall it's attached to.")]
public class HangingVines : Grass, IDeathHandler {
  public Tile tileBelow => floor.tiles[pos + Vector2Int.down];
  private Trigger triggerBelow;

  public HangingVines(Vector2Int pos) : base(pos) {
  }

  protected override void HandleEnterFloor() {
    triggerBelow = new Trigger(tileBelow.pos, HandleActorEnterBelow);
    floor.Put(triggerBelow);
  }

  protected override void HandleLeaveFloor() {
    floor.Remove(triggerBelow);
  }

  public void HandleDeath(Entity source) {
    if (appliedStatus == null) {
      Inventory inventory = new Inventory(new ItemVineWhip(1));
      inventory.TryDropAllItems(floor, tileBelow.pos);
    }
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
    ConstrictedStatusEnded();
    // simply unset the reference
    appliedStatus = null;
  }
}

[Serializable]
/// not movable
public class Trigger : Entity, IActorEnterHandler {
  /// be careful with action serialization
  public Action<Actor> action;
  private Vector2Int _pos;

  public override Vector2Int pos {
    get => _pos;
    set { }
  }

  public Trigger(Vector2Int pos, Action<Actor> action) {
    _pos = pos;
    this.action = action;
  }

  public Trigger(Vector2Int pos) : this(pos, null) {}


  public virtual void HandleActorEnter(Actor who) {
    action?.Invoke(who);
  }
}

[Serializable]
[ObjectInfo("vine-whip", flavorText: "Just the sound of it whipping through the air makes you a little nervous.")]
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
    if (target is Actor) {
      Destroy();
    }
  }

  internal override string GetStats() => "Damage is equal to number of stacks.\nDestroyed on use.";
}

[System.Serializable]
[ObjectInfo("constricted-status", flavorText: "Thick, damp vines tighten around you!")]
public class ConstrictedStatus : StackingStatus, IBaseActionModifier, IBodyMoveHandler, IDeathHandler, IBodyTakeAttackDamageHandler {
  private readonly HangingVines owner;

  public override bool isDebuff => true;
  public override StackingMode stackingMode => StackingMode.Max;
  public ConstrictedStatus(HangingVines owner, int stacks = 7) : base(stacks) {
    this.owner = owner;
  }

  public override void End() {
    owner?.ConstrictedStatusEnded();
    base.End();
  }

  public void HandleDeath(Entity source) {
    // signal back to the hanging vines that the creature died with this status on it.
    owner?.ConstrictedCreatureDied();
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
      if (actor is Player) {
        stacks--;
      }
      if (stacks <= 0) {
        return input;
      } else {
        if (actor is Player) {
          return new StruggleBaseAction(input.actor);
        } else {
          return new WaitBaseAction(input.actor);
        }
      }
    }
    return input;
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    Remove();
  }
}