using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class Player : Actor {
  public static readonly int MAX_FULLNESS = 1000;
  internal readonly Item Hands;

  public Inventory inventory { get; }
  public Equipment equipment { get; }
  /// 1000 is max fullness
  public int fullness = MAX_FULLNESS;

  internal override float turnPriority => 10;

  public Player(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    inventory = new Inventory(this, 12);
    inventory.AddItem(new ItemBarkShield());
    inventory.AddItem(new ItemBerries(3));
    inventory.AddItem(new ItemSeed(typeof(BerryBush)));
    inventory.AddItem(new ItemSeed(typeof(Wildwood)));
    inventory.AddItem(new ItemStick());
    equipment = new Equipment(this);
    Hands = new ItemHands(this);
    hp = hpMax = 12;
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
    OnStepped += HandleStepped;
    OnAttack += HandleAttack;
  }

  private void HandleLeaveFloor() {
    floor.RemoveVisibility(this);
  }

  private void HandleEnterFloor() {
    floor.AddVisibility(this);
  }

  void HandleStepped(float timeCost) {
    fullness = Math.Max(fullness - 1, 0);
    // you are now starving
    if (fullness <= 0) {
      this.TakeDamage(1, this);
    }
  }

  void HandleAttack(int damage, Actor target) {
    var item = equipment[EquipmentSlot.Weapon];
    if (item is IDurable durable) {
      Durables.ReduceDurability(durable);
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

  protected override int ModifyDamage(int damage) {
    foreach (var item in equipment.ItemsNonNull()) {
      if (item is IDamageModifier damageModifier && damage > 0) {
        damage = damageModifier.ModifyDamage(damage);
      }
    }
    return damage;
  }

  internal override int GetAttackDamage() {
    var item = equipment[EquipmentSlot.Weapon];
    if (item is IWeapon w) {
      var (min, max) = w.AttackSpread;
      return UnityEngine.Random.Range(min, max + 1);
    } else {
      Debug.Log("Attacking with a non-weapon in the weapon slot: " + item);
      return 1;
    }
  }
}
