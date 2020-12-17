using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class Player : Actor {
  public static readonly int MAX_FULLNESS = 1000;
  internal readonly Item Hands;

  public Inventory inventory { get; }
  public Equipment equipment { get; }
  public override IEnumerable<object> MyModifiers => base.MyModifiers.Concat(equipment);
  /// 1000 is max fullness
  public float fullness = MAX_FULLNESS;

  internal override float turnPriority => 10;

  public Player(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    inventory = new Inventory(this, 12);
    inventory.AddItem(new ItemBarkShield());

    equipment = new Equipment(this);
    Hands = new ItemHands(this);
    hp = hpMax = 12;
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
    OnPostStep += HandlePostStep;
    OnAttack += HandleAttack;
    OnActionPerformed += HandleActionPerformed;
    statuses.OnAdded += HandleStatusAdded;
  }

  private void HandleActionPerformed(BaseAction final, BaseAction initial) {
    // player didn't do what they intended! We should reset and give
    // player a choice.
    if (final != initial) {
      ClearTasks();
    }
    // this is pretty much a delegate whose invocation list is declarative
    foreach (var handler in Modifiers.Of<IActionPerformedHandler>(MyModifiers)) {
      handler.HandleActionPerformed(final, initial);
    }
  }

  private void HandleStatusAdded(Status status) {
    if (status is IBaseActionModifier) {
      // cancel current action
      ClearTasks();
    }
  }

  private void HandleLeaveFloor() {
    floor.RemoveVisibility(this);
  }

  private void HandleEnterFloor() {
    floor.AddVisibility(this);
  }

  void HandlePostStep(float timeCost) {
    fullness = Math.Max(fullness - timeCost, 0);
    // you are now starving
    if (fullness <= 0) {
      this.TakeDamage(1, this);
    }
  }

  void HandleAttack(int damage, Actor target) {
    var item = equipment[EquipmentSlot.Weapon];
    if (item is IDurable durable) {
      durable.ReduceDurability();
    }
    if (task is FollowPathTask) {
      task = null;
    }
  }

  public override Vector2Int pos {
    get {
      return base.pos;
    }

    set {
      GameModel model = GameModel.main;
      if (floor != null) {
        floor.RemoveVisibility(this);
      }
      base.pos = value;
      if (floor != null) {
        floor.AddVisibility(this);
      }
    }
  }

  internal void IncreaseFullness(float v) {
    int amount = (int) (v * MAX_FULLNESS);
    fullness = Mathf.Clamp(fullness + amount, 0, MAX_FULLNESS);
  }

  protected override int ModifyDamageTaken(int damage) {
    var newDamage = Modifiers.Process(Modifiers.DamageTakenModifiers(equipment.ItemsNonNull()), damage);
    return newDamage;
  }

  internal override int BaseAttackDamage() {
    var item = equipment[EquipmentSlot.Weapon];
    if (item is IWeapon w) {
      var (min, max) = w.AttackSpread;
      return UnityEngine.Random.Range(min, max + 1);
    } else {
      Debug.Log("Player attacking with a non-weapon in the weapon slot: " + item);
      return 1;
    }
  }

  public override void CatchUpStep(float lastStepTime, float time) {
    // no op for the player
  }
}
