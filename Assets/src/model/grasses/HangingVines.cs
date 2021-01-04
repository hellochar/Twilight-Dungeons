using System;
using System.Collections.Generic;
using UnityEngine;

public class HangingVines : Grass {
  private Inventory inventory = new Inventory(new ItemVineWhip(1));
  public Tile tileBelow => floor.tiles[pos + new Vector2Int(0, -1)];

  public HangingVines(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
    OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    inventory.TryDropAllItems(floor, tileBelow.pos);
    if (appliedStatus != null) {
      appliedStatus.Remove();
    }
  }

  private void HandleEnterFloor() {
    tileBelow.OnActorEnter += HandleActorEnter;
  }

  private void HandleLeaveFloor() {
    tileBelow.OnActorEnter -= HandleActorEnter;
  }

  private BoundStatus appliedStatus;
  private void HandleActorEnter(Actor who) {
    appliedStatus = new BoundStatus();
    who.statuses.Add(appliedStatus);
    TriggerNoteworthyAction();
    appliedStatus.OnRemoved += HandleStatusRemoved;
  }

  private void HandleStatusRemoved() {
    // when someone is able to break free; remove these vines
    appliedStatus = null;
    Kill();
  }
}

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
  public (int, int) AttackSpread => (1, stacks);
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  public ItemVineWhip(int stacks) {
    this.stacks = stacks;
  }

  public void OnAttack(Actor target) {
    if (!(target is Rubble)) {
      stacks--;
    }
  }

  internal override string GetStats() => "Max damage is equal to number of stacks.\nLose one stack on attack.";
}

public class BoundStatus : StackingStatus, IBaseActionModifier {
  public override bool isDebuff => true;
  public override StackingMode stackingMode => StackingMode.Max;
  public BoundStatus() {
    stacks = 3;
  }

  public override string Info() => $"You must break free of vines before you can move or attack!\n{(int)(stacks / 3.0f * 100)}% bound.";

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