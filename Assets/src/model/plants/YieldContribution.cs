using System;
using System.Linq;

public delegate YieldContribution YieldContributionRule(Plant plant);

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

}

