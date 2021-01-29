using System;
using System.Collections.Generic;
using UnityEngine;

[ObjectInfo(description: "Constricts any creature that walks into its hook.\nYou may destroy the Hanging Vines by tapping the stem.")]
public class HangingVines : Grass {
  private Inventory inventory = new Inventory(new ItemVineWhip(1));
  public Tile tileBelow => floor.tiles[pos + new Vector2Int(0, -1)];
  private HangingVinesTrigger trigger;

  public HangingVines(Vector2Int pos) : base(pos) {
    trigger = new HangingVinesTrigger(this);
    OnDeath += HandleDeath;
  }

  protected override void HandleEnterFloor() {
    floor.Put(trigger);
  }

  protected override void HandleLeaveFloor() {
    floor.Remove(trigger);
  }

  private void HandleDeath() {
    inventory.TryDropAllItems(floor, tileBelow.pos);
    if (appliedStatus != null) {
      appliedStatus.Remove();
    }
  }

  private BoundStatus appliedStatus;
  public void HandleActorEnterBelow(Actor who) {
    appliedStatus = new BoundStatus();
    who.statuses.Add(appliedStatus);
    OnNoteworthyAction();
    appliedStatus.OnRemoved += HandleStatusRemoved;
  }

  private void HandleStatusRemoved() {
    // when someone is able to break free; remove these vines
    appliedStatus = null;
    Kill();
  }
}

class HangingVinesTrigger : Entity, IActorEnterHandler {
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

  public void OnAttack(Body target) {
    if (!(target is Rubble)) {
      Destroy();
    }
  }

  internal override string GetStats() => "Damage is equal to number of stacks.\nDestroyed on use.";
}

public class BoundStatus : StackingStatus, IBaseActionModifier {
  public override bool isDebuff => true;
  public override StackingMode stackingMode => StackingMode.Max;
  public BoundStatus() {
    stacks = 3;
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