using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static YieldContribution;

[Serializable]
public abstract class Plant : Body, IHideInSidebar, IDaySteppable {
  [field:NonSerialized] /// controller only
  public event Action OnHarvested;

  public float percentGrown {
    get {
      if (stage.NextStage == null) {
        return 1;
      } else {
        return stage.percentGrown;
      }
    }
  }

  private PlantStage _stage;
  public ItemFertilizer fertilizer;

  internal bool isMatured => percentGrown >= 1;

  public PlantStage stage {
    get => _stage;
    set {
      _stage = value;
      _stage.BindTo(this);
      /// hack - apply soil improvements here
      if (IsSurroundedByGrass()) {
        var existingOptions = new List<Inventory>(_stage.harvestOptions);
        _stage.harvestOptions.Clear();
        _stage.plant = null;
        _stage.BindTo(this);
        for (int i = 0; i < _stage.harvestOptions.Count; i++) {
          foreach (var item in existingOptions[i]) {
            _stage.harvestOptions[i].AddItem(item, null, true);
          }
        }
      }

      /// hack - apply fertilizer here
      if (fertilizer != null) {
        foreach (var inventory in _stage.harvestOptions) {
          foreach (var item in inventory.ItemsNonNull()) {
            if (item is IWeapon w) {
              fertilizer.Imbue(w);
            }
          }
        }
      }
    }
  }

  public override string displayName => $"{base.displayName}{ (stage.NextStage == null ? "" : " (" + stage.name + ")") }{ (IsSurroundedByGrass() ? " 2x" : "") }";

  public static Vector2Int[] PlantShape = new Vector2Int[] { Vector2Int.zero, Vector2Int.up };
  public override Vector2Int[] shape => PlantShape;
  public int dayCreated;
  public int dayAge => GameModel.main.day - dayCreated;
  public Plant(Vector2Int pos, PlantStage stage) : base(pos) {
    this.stage = stage;
    this.hp = this.baseMaxHp = 1;
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    dayCreated = GameModel.main.day;
    RecomputeAndRefillYield();
    stage.RestockHarvests(yield);
  }
  public bool IsSurroundedByGrass() {
    return floor == null ? false : floor.GetDiagonalAdjacentTiles(pos).Where(t => t.grass != null).Count() >= 8;
  }

  public void GoNextStage() {
    if (stage.NextStage != null) {
      stage = stage.NextStage;
    }
  }

  public virtual int lifetime => 10;

  public void StepDay() {
    if (dayAge > lifetime) {
      KillSelf();
      return;
    }
    // harvestedToday = false;
    if (stage.NextStage != null) {
      stage.GrowTowardsNextStage();
    }
    RecomputeAndRefillYield();
    stage.RestockHarvests(yield);
  }

  public static YieldContributionRule[] BaseContributionRules => new YieldContributionRule[] {
    AgeYieldContribution,
    NearGrassYieldContribution,
    SoilWateredYieldContribution,
    SoilNutrientYieldContribution
  };

  public virtual YieldContributionRule[] contributionRules => BaseContributionRules;

  public List<YieldContribution> latestContributions = new List<YieldContribution>();

  public void RecomputeAndRefillYield() {
    latestContributions.Clear();
    yield = YieldContributionUtils.Recompute(this, contributionRules, latestContributions);
  }

  public int yield = 1;

  protected virtual bool isFreeHarvest => floor.depth > 0;
  internal void Harvest(int choiceIndex) {
    // if (harvestedToday) {
    //   throw new CannotPerformActionException("Already harvested today!");
    // }
    // harvestedToday = true;

    var player = GameModel.main.player;
#if experimental_actionpoints
    // if (!isFreeHarvest) {
    //   player.UseActionPointOrThrow();
    // }
#endif
    var inventory = stage.harvestOptions[choiceIndex];
    var boughtItem = inventory.ItemsNonNull().FirstOrDefault();
    if (boughtItem != null) {
      int cost = YieldContributionUtils.GetCost(boughtItem);
      if (yield >= cost) {
        yield -= cost;
        // var splitItem = boughtItem.Split(1);
        var item = boughtItem.GetType().GetConstructor(new Type[0]).Invoke(new object[0]) as Item;
        var itemOnGround = new ItemOnGround(pos, item, pos);
        floor.Put(itemOnGround);
        OnHarvested?.Invoke();
      }
    }

    // stage.harvestOptions[choiceIndex].TryDropAllItems(floor, pos);
    // OnHarvested?.Invoke();
    // Kill(player);
  }
}
