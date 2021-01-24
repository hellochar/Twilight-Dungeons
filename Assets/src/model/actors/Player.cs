using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Player : Actor {
  public int deepestDepthVisited = 1;
  internal readonly Item Hands;

  public Inventory inventory { get; }
  public Equipment equipment { get; }
  public override IEnumerable<object> MyModifiers => base.MyModifiers.Concat(equipment);

  public override float turnPriority => 10;
  public int water = 0;

  public Player(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    inventory = new Inventory(12);

    equipment = new Equipment(this);
    Hands = new ItemHands(this);
    hp = baseMaxHp = 12;
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
    OnAttack += HandleAttack;
    OnMove += HandleMove;
    OnMoveFailed += HandleMoveFailed;
    OnTakeAttackDamage += HandleTakeDamage;
    OnActionPerformed += HandleActionPerformed;
    statuses.OnAdded += HandleStatusAdded;
  }

  private HashSet<Actor> lastVisibleEnemies = new HashSet<Actor>();

  private void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    if (floor != null) {
      floor.RemoveVisibility(this, oldPos);
      floor.AddVisibility(this, newPos);
    }
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
    var depth = floor.depth;
    if (depth > deepestDepthVisited) {
      deepestDepthVisited = depth;
    }
  }

  void HandleAttack(int damage, Body target) {
    var item = equipment[EquipmentSlot.Weapon];
    foreach (var handler in Modifiers.Of<IAttackHandler>(this)) {
      handler.OnAttack(target);
    }
    if (item is IDurable durable && target is Actor) {
      GameModel.main.EnqueueEvent(durable.ReduceDurability);
    }
    if (task is FollowPathTask) {
      task = null;
    }
  }

  internal override (int, int) BaseAttackDamage() {
    var item = equipment[EquipmentSlot.Weapon];
    if (item is IWeapon w) {
      return w.AttackSpread;
    } else {
      Debug.Log("Player attacking with a non-weapon in the weapon slot: " + item);
      return (1, 1);
    }
  }

  public IEnumerable<Actor> ActorsInSight(Faction faction) => floor.ActorsInCircle(pos, visibilityRange).Where((a) => a.isVisible && a.faction == faction);

  /// <summary>Return true if there are any enemies in vision range.</summary>
  public bool IsInCombat() {
    return ActorsInSight(Faction.Enemy).Any();
  }
}
