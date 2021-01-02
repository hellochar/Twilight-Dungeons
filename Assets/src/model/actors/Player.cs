using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Player : Actor {
  internal readonly Item Hands;

  public Inventory inventory { get; }
  public Equipment equipment { get; }
  public override IEnumerable<object> MyModifiers => base.MyModifiers.Concat(equipment);
  /// 0 to 1
  public float fullness = 1;

  internal override float turnPriority => 10;
  public int water = 0;

  public Player(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    inventory = new Inventory(12);

    equipment = new Equipment(this);
    Hands = new ItemHands(this);
    hp = baseMaxHp = 12;
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
    OnPostStep += HandlePostStep;
    OnAttack += HandleAttack;
    OnMove += HandleMove;
    OnMoveFailed += HandleMoveFailed;
    OnTakeAttackDamage += HandleTakeDamage;
    OnActionPerformed += HandleActionPerformed;
    statuses.OnAdded += HandleStatusAdded;
  }

  private HashSet<Actor> lastVisibleEnemies = new HashSet<Actor>();
  private void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    GameModel.main.EnqueueEvent(() => {
      var visibleEnemies = new HashSet<Actor>(ActorsInSight(Faction.Enemy));
      // if there's a newly visible enemy from last turn, cancel the current move task
      var isNewlyVisibleEnemy = visibleEnemies.Any((enemy) => !lastVisibleEnemies.Contains(enemy));
      if (isNewlyVisibleEnemy && task is FollowPathTask) {
        ClearTasks();
      }
      lastVisibleEnemies = visibleEnemies;
    });
  }

  private void HandleTakeDamage(int arg1, int arg2, Actor arg3) {
    if (task is FollowPathTask) {
      ClearTasks();
    }
  }

  private void HandleMoveFailed(Vector2Int arg1, Vector2Int arg2) {
    ClearTasks();
  }

  private void HandleActionPerformed(BaseAction final, BaseAction initial) {
    // player didn't do what they intended! We should reset and give
    // player a choice.
    if (final != initial) {
      ClearTasks();
    }
    // this is pretty much a delegate whose invocation list is declarative
    foreach (var handler in Modifiers.Of<IActionPerformedHandler>(this)) {
      handler.HandleActionPerformed(final, initial);
    }
  }

  private void HandleStatusAdded(Status status) {
    if (status.isDebuff) {
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
    // fullness = Math.Max(fullness - timeCost / 1000f, 0);
    // you are now starving
    if (fullness <= 0) {
      statuses.RemoveOfType<HungryStatus>();
      statuses.Add(new StarvingStatus());
    } else if (fullness <= 0.1f) {
      statuses.RemoveOfType<StarvingStatus>();
      statuses.Add(new HungryStatus());
    } else {
      statuses.RemoveOfType<HungryStatus>();
      statuses.RemoveOfType<StarvingStatus>();
    }
  }

  void HandleAttack(int damage, Actor target) {
    var item = equipment[EquipmentSlot.Weapon];
    if (item is IDurable durable && !(target is Rubble)) {
      durable.ReduceDurability();
    }
    if (item is IAttackHandler handler) {
      handler.OnAttack(target);
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
    fullness += v;
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

  public IEnumerable<Actor> ActorsInSight(Faction faction) => floor.ActorsInCircle(pos, visibilityRange).Where((a) => a.isVisible && a.faction == faction);

  /// <summary>Return true if there are any enemies in vision range.</summary>
  public bool IsInCombat() {
    return ActorsInSight(Faction.Enemy).Any();
  }
}

internal class StarvingStatus : Status {
  private Player player => (Player) actor;
  int cooldown = 0;

  public override string Info() => $"You're starving! You will regularly take damage.";

  public override void Step() {
    if (cooldown <= 0) {
      player.TakeDamage(1);
      cooldown = 50;
    }
    cooldown--;
  }

  public override void Stack(Status other) {}
}

internal class HungryStatus : Status {
  private Player player => (Player) actor;

  public override string Info() => $"You have {FullnessPercent}% food left. Eat something!";

  public string FullnessPercent => Mathf.Ceil(player.fullness * 100).ToString();

  public override void Stack(Status other) { }
}