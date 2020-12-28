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
    inventory.DropRandomlyOntoFloorAround(floor, tileBelow.pos);
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
    stacks--;
  }

  internal override string GetStats() => "Max damage is equal to number of stacks.\nLose one stack on attack.";
}

// Called when this weapon is used for an attack
public interface IAttackHandler {
  void OnAttack(Actor target);
}