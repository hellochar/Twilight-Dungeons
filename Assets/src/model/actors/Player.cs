using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Player : Actor, IBodyMoveHandler, IAttackHandler, IBodyTakeAttackDamageHandler, IActionPerformedHandler, IStatusAddedHandler {
  private float timeLastLostWater = 0;
  public float water = 0;
  public int deepestDepthVisited = 1;
  internal readonly ItemHands Hands;
  [NonSerialized] /// lazily instantiated
  private HashSet<Actor> lastVisibleEnemies;
  public Inventory inventory { get; }
  public Equipment equipment { get; }

  public override IEnumerable<object> MyModifiers => base.MyModifiers.Concat(equipment);
  public override float turnPriority => 10;

  public Player(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    inventory = new Inventory(12);

    equipment = new Equipment(this);
    Hands = new ItemHands(this);
    hp = baseMaxHp = 12;
  }

  [OnSerializing]
  private void OnSerializing(StreamingContext context) {
    /// clear player tasks on save
    ClearTasks();
  }

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    if (floor != null) {
      floor.RemoveVisibility(this, oldPos);
      floor.AddVisibility(this, newPos);
    }
    GameModel.main.EnqueueEvent(() => {
      var visibleEnemies = new HashSet<Actor>(ActorsInSight(Faction.Enemy));
      // if there's a newly visible enemy from last turn, cancel the current move task
      var isNewlyVisibleEnemy = visibleEnemies.Any((enemy) => lastVisibleEnemies != null && !lastVisibleEnemies.Contains(enemy));
      if (isNewlyVisibleEnemy && task is FollowPathTask) {
        ClearTasks();
      }
      lastVisibleEnemies = visibleEnemies;
    });
  }

  public void HandleTakeAttackDamage(int arg1, int arg2, Actor arg3) {
    if (task is FollowPathTask) {
      ClearTasks();
    }
  }

  protected override void OnMoveFailed(Vector2Int target) {
    ClearTasks();
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    GameModel.main.EnqueueEvent(() => {
      if (water > 0 && GameModel.main.time - timeLastLostWater > 10) {
        water -= 0.01f;
        timeLastLostWater = GameModel.main.time;
      }
    });
    // player didn't do what they intended! We should reset and give
    // player a choice.
    if (final != initial) {
      ClearTasks();
    }
  }

  public void HandleStatusAdded(Status status) {
    if (status.isDebuff) {
      // cancel current action
      ClearTasks();
    }
  }

  protected override void HandleLeaveFloor() {
    floor.RemoveVisibility(this);
  }

  protected override void HandleEnterFloor() {
    floor.AddVisibility(this);
    var depth = floor.depth;
    if (depth > deepestDepthVisited) {
      deepestDepthVisited = depth;
    }
  }

  public void OnAttack(int damage, Body target) {
    var item = equipment[EquipmentSlot.Weapon];
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
