using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using static YieldContribution;

public delegate void OnNoteworthyAction();

[Serializable]
public class Grass : Entity, IDaySteppable {
  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving grass
    set { }
  }
  public virtual object BodyModifier { get; protected set; }

  [NonSerialized] /// controller only
  public OnNoteworthyAction OnNoteworthyAction = delegate {};
  [OnDeserialized]
  void HandleDeserialized() {
    OnNoteworthyAction = delegate {};
  }

  // public override string description => (floor == null || floor is HomeFloor) ?
  //   HomeDescription() : base.description;

  static Dictionary<Vector2Int, string> directionNames = new Dictionary<Vector2Int, string>() {
    [Vector2Int.left] = "left",
    [Vector2Int.down] = "down",
    [Vector2Int.right] = "right",
    [Vector2Int.up] = "up",
  };

  string HomeDescription() {
    var homeItem = this.GetHomeItem();
    string description;
    string desc1 = floor == null ? "" : synergy.IsSatisfied(this) ? $"Synergies satisfied! Reproducing." : "";
    description = $@"{desc1}
Synergies: {string.Join(", ", synergy.offsets.Select(o => directionNames.GetValueOrDefault(o) ?? o.ToString()))}.".Trim();
    return description;
  }

  public static void Eat(Player p) {
    p.hunger -= 25;
  }

  public Grass(Vector2Int pos) : base() {
    this._pos = pos;
  }

  public void BecomeItemInInventory(Item innerItem, Player player) {
    var floor = this.floor;
    var item = new ItemVisibleBox(innerItem);
    Kill(actor);
    if (!player.inventory.AddItem(item, this)) {
      floor.Put(new ItemOnGround(pos, item, pos));
    }
  }

  public static YieldContributionRule[] BaseGrassContributionRules => new YieldContributionRule[] {
    // AgeYieldContribution,
    // NearGrassYieldContribution,
    SoilWateredYieldContribution,
    SoilNutrientYieldContribution
  };

  public virtual YieldContributionRule[] contributionRules => BaseGrassContributionRules;

  public List<YieldContribution> latestContributions = new List<YieldContribution>();

  public virtual Synergy synergy => Synergy.SynergyMapping.GetValueOrDefault(GetType()) ?? Synergy.Never;

  public virtual void StepDay() {
    // justPlanted = false;
    // readyToExpand = true;
    // var num = synergy.IsSatisfied(this) ? 2 : 1;
    // var hasSynergy = synergy.IsSatisfied(this);
    // if (hasSynergy) {
    //   floor.Put(new ItemOnGround(pos, new ItemGrass(GetType(), 1), pos));
    // }
    // if (justPlanted == false) {
    //  SpreadAutomatically();
    // }
  }

  protected void SpreadAutomatically() {
    var canOccupyMethod = GetType().GetMethod("CanOccupy");
    bool canOccupy(Tile t) {
      if (canOccupyMethod != null) {
        return (bool) canOccupyMethod.Invoke(null, new object[] { t });
      }
      return t is Ground;
    }
    var openSpot = Util.RandomPick(floor.GetAdjacentTiles(pos).Where(t => canOccupy(t) && t.grass == null && t.CanBeOccupied()));
    if (openSpot != null) {
      var constructor = GetType().GetConstructor(new Type[1] { typeof(Vector2Int) });
      var newGrass = (Grass)constructor.Invoke(new object[] { openSpot.pos });
      grass.floor.Put(newGrass);
    }
  }

  // public override void GetAvailablePlayerActions(List<MethodInfo> methods) {
  //   // if (readyToExpand) {
  //     // methods.Add(GetType().GetMethod("Harvest"));
  //     // methods.Add(GetType().GetMethod("Duplicate"));
  //   // }
  // }

  public virtual void Harvest(Player player) {
    if (GameModel.main.player.inventory.AddItem(new ItemGrass(GetType()), this)) {
      KillSelf();
    }
  }

  // [PlayerAction]
  // public void Destroy() {
  //   floor.Remove(this);
  // }

  // [PlayerAction]
  // public void Harvest() {
  //   var item = this.GetHomeItem();
  //   if (item == null) {
  //     return;
  //   }

  //   if (synergy.IsSatisfied(this)) {
  //     item.stacks *= 2;
  //   }
  //   if (!GameModel.main.player.inventory.AddItem(item, this)) {
  //     floor.Put(new ItemOnGround(pos, item));
  //   }
  //   floor.Remove(this);
  // }

  // public void Harvest() {
  //   var player = GameModel.main.player;
  //   var numStacks = synergy.IsSatisfied(this) ? 3 : 2;
  //   bool bSuccess = player.inventory.AddItem(new ItemGrass(GetType(), numStacks), this);
  //   if (!bSuccess) {
  //     throw new CannotPerformActionException("Inventory full!");
  //   }
  //   Kill(player);
  // }

  // [PlayerAction]
  // public void PickUp() {
  //   if (justPlanted) {
  //     throw new CannotPerformActionException("Just planted!");
  //   }
  //   var player = GameModel.main.player;
  //   bool bSuccess = player.inventory.AddItem(new ItemGrass(GetType(), 1), this);
  //   if (bSuccess) {
  //     // we're *not* killing the entity
  //     floor.Remove(this);
  //   }
  // }

  // [PlayerAction]
  // public void Harvest() {
  //   if (justPlanted) {
  //     throw new CannotPerformActionException("Just planted!");
  //   }
  //   var player = GameModel.main.player;
  //   bool bSuccess = player.inventory.AddItem(new ItemGrass(GetType(), 1), this);
  //   if (bSuccess) {
  //     // we're *not* killing the entity
  //     floor.Remove(this);
  //   }
  // }

  // private bool justPlanted = true;
  // public bool readyToExpand = false;
}
