using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

  public HomeFloor home => floor as HomeFloor;
  public Soil soil => home.soils[pos];

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

  public virtual int lifetime => 8;

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

[Serializable]
public class YieldContribution {
  public int bonus;
  public bool active;
  public string description;
  public string ToDisplayString() {
    string s = $"{bonus.ToString("+0")} - {description}";
    if (!active) {
      s = $"<color=#ffffff99>{s}</color>";
    }
    return s;
  }
}

public delegate YieldContribution YieldContributionRule(Plant plant);

  // "+3 - Age {2}.";
  // "+1 - Soil watered.";
  // "+4 - Soil has {4} nutrients.";
  // "+3 - Next to {3} Grasses.";

  public static YieldContribution AgeYieldContribution(Plant p) => new YieldContribution {
    active = true,
    bonus = 3 + p.dayAge,
    description = $"Age {p.dayAge}.",
  };

  public static YieldContribution NearGrassYieldContribution(Plant p) {
    var nearbyGrasses = p.floor.GetAdjacentTiles(p.pos).Select(t => t.grass).Where(t => t != null);
    var numNearbyGrasses = nearbyGrasses.Count();
    return new YieldContribution {
      active = numNearbyGrasses > 0,
      bonus = numNearbyGrasses,
      description = $"Next to {numNearbyGrasses} Grasses.",
    };
  }

  public static YieldContribution SoilWateredYieldContribution(Plant p) {
    var soil = p.soil;
    var active = soil?.watered ?? false;
    return new YieldContribution {
      active = active,
      bonus = 3,
      description = "Soil watered."
    };
  }

  public static YieldContribution SoilNutrientYieldContribution(Plant p) {
    var soil = p.soil;
    var nutrient = soil?.nutrient ?? 0;
    var active = nutrient > 0;
    return new YieldContribution {
      active = active,
      bonus = nutrient,
      description = $"Soil has {nutrient} nutrients."
    };
  }

  public static YieldContributionRule[] BaseContributionRules => new Plant.YieldContributionRule[] {
    AgeYieldContribution,
    NearGrassYieldContribution,
    SoilWateredYieldContribution,
    SoilNutrientYieldContribution
  };

  public virtual YieldContributionRule[] contributionRules => BaseContributionRules;

  public List<YieldContribution> latestContributions = new List<YieldContribution>();

  public void RecomputeAndRefillYield() {
    var totalYield = 0;
    latestContributions.Clear();
    foreach (var Rule in contributionRules) {
      var contribution = Rule.Invoke(this);
      totalYield += contribution.active ? contribution.bonus : 0;
      latestContributions.Add(contribution);
    }
    yield = totalYield;
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
      int cost = (int) boughtItem.GetType().GetField("yieldCost").GetValue(null);
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
