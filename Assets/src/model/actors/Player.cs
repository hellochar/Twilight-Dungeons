using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Only you can use and equip items.\nOnly you can take stairs.", flavorText: "Though your illness makes you physically weak, your knowledge of flora and fauna helps you navigate these strange caves.")]
public class Player : Actor, IBodyMoveHandler, IAttackHandler,
  ITakeAnyDamageHandler, IDealAttackDamageHandler, IActionPerformedHandler,
  IKillEntityHandler, IStatusAddedHandler, IHideInSidebar, IDeathHandler {
  private float timeLastLostWater = 0;
  private int m_water;
  public int water {
    get => m_water;
    set {
      var diff = value - m_water;
      m_water = value;
      if (diff > 0) {
        GameModel.main.stats.waterCollected += diff;
      }
      OnChangeWater?.Invoke(diff);
    }
  }

#if experimental_actionpoints
  public int actionPoints = 3;

  public int maxActionPoints = 3;
#endif

  public bool isCamouflaged => Modifiers.Of<IPlayerCamouflage>(this).Any();

  // go to full HP and remove all debuffs
  internal void Replenish() {
    // doesn't count as a heal
    hp = maxHp;
    var debuffs = statuses.list.Where(s => s.isDebuff).ToArray();
    foreach (var debuff in debuffs) {
      statuses.Remove(debuff);
    }
  }

  internal readonly ItemHands Hands;
  [NonSerialized] /// lazily instantiated
  private HashSet<Actor> lastVisibleEnemies;
  public Inventory inventory { get; }
  public Equipment equipment { get; }

  public override IEnumerable<object> MyModifiers => base.MyModifiers.Concat(equipment);

  public override float turnPriority => 10;
  [field:NonSerialized] /// controller only, int delta
  public event Action<int> OnChangeWater;

  public Player(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    inventory = new Inventory(10);
#if experimental_grasscovering
    inventory.AddItem(new ItemShovel());
#endif
#if experimental_actionpoints
    inventory.AddItem(new ItemPlaceableEntity(new Campfire(new Vector2Int())).RequireSpace());
#endif

    equipment = new Equipment(this);
    Hands = new ItemHands(this);
    hp = baseMaxHp = 12;
  }

  [OnSerializing]
  private void OnSerializing(StreamingContext context) {
    /// clear player tasks on save
    ClearTasks();
  }

  internal void StepDay() {
#if experimental_actionpoints
    if (maxActionPoints < 10) {
      maxActionPoints++;
    }
    actionPoints = maxActionPoints;
#endif
  }

  public override float GetActionCost(BaseAction action) {
    if (floor is HomeFloor) {
      return 0;
    }
    return base.GetActionCost(action);
  }

  public void HandleDeath(Entity source) {
    GameModel.main.GameOver(false, source);
  }

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    floor.RecomputeVisibility();
    UpdateVisibleEnemies();
  }

  private void UpdateVisibleEnemies() {
    GameModel.main.EnqueueEvent(() => {
      var visibleEnemies = new HashSet<Actor>(GetVisibleActors(Faction.Enemy));
      // if there's a newly visible enemy from last turn, cancel the current move task
      var isNewlyVisibleEnemy = visibleEnemies.Any((enemy) => lastVisibleEnemies != null && !lastVisibleEnemies.Contains(enemy));
      if (isNewlyVisibleEnemy && task is FollowPathTask) {
        ClearTasks();
      }
      lastVisibleEnemies = visibleEnemies;
    });
  }

  public void HandleTakeAnyDamage(int damage) {
    GameModel.main.stats.damageTaken += damage;
    if (task is FollowPathTask) {
      ClearTasks();
    }
  }

  public void HandleDealAttackDamage(int damage, Body target) {
    GameModel.main.stats.damageDealt += damage;
  }

  public void OnKill(Entity entity) {
    if (entity is Actor a && a.faction == Faction.Enemy) {
      GameModel.main.stats.enemiesDefeated += 1;
    }
  }

  protected override void OnMoveFailed(Vector2Int target) {
    ClearTasks();
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    GameModel.main.EnqueueEvent(() => {
      if (GameModel.main.time - timeLastLostWater > 10) {
        if (water > 0) {
          water -= 1;
        }
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

  protected override void HandleEnterFloor() {
    floor.RecomputeVisibility();
    UpdateVisibleEnemies();
  }

#if experimental_equipmentperfloor
  protected override void OnFloorChanged(Floor newFloor, Floor oldFloor) {
    base.OnFloorChanged(newFloor, oldFloor);
    bool shouldDestroy = newFloor.depth != 0 || oldFloor.depth != 0;
#if experimental_alwaysgohome
    shouldDestroy = newFloor.depth == 0;
#endif
    if (shouldDestroy) {
      foreach(var e in equipment) {
        if (e is IDurable && !(e is ISticky)) {
          e.Destroy();
        }
      }
    }
  }
#endif

  public void OnAttack(int damage, Body target) {
    var item = equipment[EquipmentSlot.Weapon];
    if (!(target is Destructible)) {
      if (item is IDurable durable) {
        GameModel.main.EnqueueEvent(durable.ReduceDurability);
      } else if (item is IStackable s) {
        GameModel.main.EnqueueEvent(() => s.stacks--);
      }
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

  public IEnumerable<Tile> GetVisibleTiles() => floor.tiles.Where(t => t.isVisible);

  // Actors that are Visible
  public IEnumerable<Actor> GetVisibleActors(Faction faction) => floor.bodies.Where((b) => b is Actor a && a.isVisible && faction.HasFlag(a.faction)).Cast<Actor>();

  /// <summary>Return true if there are any enemies in vision range.</summary>
  public bool IsInCombat() {
    return GetVisibleActors(Faction.Enemy).Any();
  }
}