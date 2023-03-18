using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Only you can use and equip items.\nOnly you can take stairs.", flavorText: "Though your illness makes you physically weak, your knowledge of flora and fauna helps you navigate these strange caves.")]
public class Player : Actor, IBodyMoveHandler, IAttackHandler,
  ITakeAnyDamageHandler, IDealAttackDamageHandler, IActionPerformedHandler,
  IKillEntityHandler, IStatusAddedHandler, IHideInSidebar, IDeathHandler
#if experimental_useplantondeath
  , IDeathInterceptor
#endif
{
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

  private int m_organicMatter;
  public int organicMatter {
    get => m_organicMatter;
    set {
      var diff = value - m_organicMatter;
      m_organicMatter = value;
      OnChangeOrganicMatter?.Invoke(diff);
    }
  }

#if experimental_actionpoints
  public int actionPoints = 6;
  public int maxActionPoints = 6;
#endif

  public void UseActionPointOrThrow(int num = 1) {
    #if experimental_actionpoints
    if (actionPoints < num) {
      throw new CannotPerformActionException($"Need {num} Action Points!");
    }
    if (floor.depth != 0) {
      throw new CannotPerformActionException("Go home first!");
    }
    actionPoints -= num;
    #endif
  }

  public void UseResourcesOrThrow(int water = 0, int organicMatter = 0, int actionPoints = 0) {
    if (this.water < water) {
      throw new CannotPerformActionException($"Need <color=lightblue>{water}</color> water!");
    }
    if (this.organicMatter < organicMatter) {
      throw new CannotPerformActionException($"Need <color=green>{organicMatter}</color> organic matter!");
    }
#if experimental_actionpoints
    if (this.actionPoints < actionPoints) {
      throw new CannotPerformActionException($"Need {actionPoints} Action Points!");
    }
#endif
    if (actionPoints > 0 && floor.depth != 0) {
      throw new CannotPerformActionException("Go home first!");
    }
    this.water -= water;
    this.organicMatter -= organicMatter;
#if experimental_actionpoints
    this.actionPoints -= actionPoints;
#endif
  }


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

  [field:NonSerialized] /// controller only, int delta
  public event Action<int> OnChangeOrganicMatter;

  public Player(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    inventory = new Inventory(12);
    inventory.allowDragAndDrop = true;

#if experimental_actionpoints
    // // inventory.AddItem(new ItemSoil());
    // inventory.AddItem(new ItemPlaceableEntity(new CraftingStation(new Vector2Int())));
    // inventory.AddItem(new ItemPlaceableEntity(new Bedroll(new Vector2Int())));
    inventory.AddItem(new ItemShovel(3));
    inventory.AddItem(new ItemCreatureFood());
    inventory.AddItem(new ItemStick(3));
    // inventory.AddItem(new ItemPlaceableEntity(new Campfire(new Vector2Int())));
    // inventory.AddItem(new ItemPlaceableEntity(new Composter(new Vector2Int())));
    // inventory.AddItem(new ItemPlaceableEntity(new SoilMixer(new Vector2Int())));
    // inventory.AddItem(new ItemSeed(typeof(Wildwood), 1));
    // inventory.AddItem(new ItemGrass(typeof(Guardleaf), 1));
    // inventory.AddItem(new ItemGrass(typeof(Bladegrass), 1));
    // inventory.AddItem(new ItemGrass(typeof(Violets), 1));
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

#if experimental_actionpoints
  [PlayerAction]
  public void Sleep() {
    // GameModel.main.player.Heal(0);
    GameModel.main.GoNextDay();
  }
#endif

  public void ReplenishActionPoints() {
    var numDays = GameModel.main.day;
#if experimental_actionpoints
    // maxActionPoints = 3;
    maxActionPoints = Mathf.Clamp(5 + numDays / 2, 3, 8);
    if (hp == maxHp)  {
      maxActionPoints += 1;
    }
    // var anyEnemies = GameModel.main.cave.bodies.Where(b => b is AIActor a && a.faction == Faction.Enemy).Any();
    // if (!anyEnemies) {
    //   maxActionPoints += 1;
    // }
    // var numHelpers = floor.bodies.Where(t => t is AIActor a).Count();
    // maxActionPoints += numHelpers;
    actionPoints = maxActionPoints;
#endif
  }

  public override float GetActionCost(BaseAction action) {
#if !experimental_cavenetwork
    if (floor is HomeFloor) {
      return 0;
    }
#endif
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
#if experimental_useplantondeath
    // hack - open these back up
    foreach (var trigger in floor.triggers.Where(t => t is WallTrigger)) {
      floor.Put(new Ground(trigger.pos));
    }
#endif
    UpdateVisibleEnemies();
  }

#if experimental_equipmentperfloor
  protected override void OnFloorChanged(Floor newFloor, Floor oldFloor) {
    base.OnFloorChanged(newFloor, oldFloor);
    bool shouldDestroy = newFloor.depth != 0 || oldFloor.depth != 0;
#if experimental_actionpoints
    shouldDestroy = newFloor.depth == 0;
#endif
    if (shouldDestroy) {
      foreach(var e in equipment) {
        if (e.disjoint && !(e is ISticky)) {
          e.Destroy();
        }
      }
    }
  }
#endif

  public void OnAttack(int damage, Body target) {
    var item = equipment[EquipmentSlot.Weapon];
    if (!(target is Destructible)) {
      if (item != null && item != Hands) {
#if !experimental_equipmentperfloor
        GameModel.main.EnqueueEvent(() => item.stacks--);
#endif
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

  public IEnumerable<Tile> GetVisibleTiles() => floor.tiles.Where(t => t.isExplored);

  // Actors that are Visible
  public IEnumerable<Actor> GetVisibleActors(Faction faction) => floor.bodies.Where((b) => b is Actor a && a.isVisible && faction.HasFlag(a.faction)).Cast<Actor>();

  /// <summary>Return true if there are any enemies in vision range.</summary>
  public bool IsInCombat() {
    return GetVisibleActors(Faction.Enemy).Any();
  }

#if experimental_useplantondeath
  public bool InterceptDeath(Entity source) {
    // check if you have any plants left at home
    var maturePlants = GameModel.main.home.plants.Where(p => p.isMatured);
    var firstPlant = maturePlants.FirstOrDefault();
    if (firstPlant != null) {
      // put the player back home, having used up the plant - you get no rewards for it!
      firstPlant.Kill(this);
      inventory.TryDropAllItems(floor, pos);
      foreach(var actor in floor.Enemies()) {
        actor.SetTasks(new SleepTask(actor));
      }
      // this will *not* trigger a new day
      GameModel.main.PutPlayerAt(0, firstPlant.pos);
      hp = maxHp;
      foreach (var status in new List<Status>(statuses.list)) {
        statuses.Remove(status);
      }
      // give the player a new seed
      var newSeedPos = floor.BreadthFirstSearch(pos, ItemOnGround.CanOccupy).Skip(1).First().pos;
      floor.Put(new ItemOnGround(newSeedPos, new ItemSeed(firstPlant.GetType(), 1), pos));
      return true;
    }
    return false;
  }
#endif
}

[Serializable]
public class PlayerInventory : Inventory {
  public PlayerInventory(int cap) : base(cap) { }

  internal override bool AddItem(Item item, int slot, Entity source = null) {
    if (item is ItemOfPiece || item is ItemGrass || item is ItemPlaceableEntity || item is ItemBox) {
      if (GameModel.main.currentFloor == GameModel.main.home) {
        return base.AddItem(item, slot, source);
      } else {
        // put pieces at home base automatically
        GameModel.main.home.Put(new ItemOnGround(GameModel.main.home.center, item));
        return true;
      }
    }
    return base.AddItem(item, slot, source);
  }
}