using System;
using System.Collections.Generic;
using System.Linq;

public delegate YieldContribution YieldContributionRule(Entity e);

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

  public static YieldContribution AgeYieldContribution(Entity e) => new YieldContribution {
    active = true,
    bonus = 3 + ((Plant)e).dayAge,
    description = $"Age {((Plant)e).dayAge}.",
  };

  public static YieldContribution NearGrassYieldContribution(Entity p) {
    var nearbyGrasses = p.floor.GetAdjacentTiles(p.pos).Select(t => t.grass).Where(t => t != null);
    var numNearbyGrasses = nearbyGrasses.Count();
    return new YieldContribution {
      active = numNearbyGrasses > 0,
      bonus = numNearbyGrasses,
      description = $"Next to {numNearbyGrasses} Grasses.",
    };
  }

  public static YieldContribution SoilWateredYieldContribution(Entity p) {
    var soil = p.soil;
    var active = soil?.watered ?? false;
    return new YieldContribution {
      active = active,
      bonus = 3,
      description = "Soil watered."
    };
  }

  public static YieldContribution SoilNutrientYieldContribution(Entity p) {
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

public static class YieldContributionUtils {
  public static int GetCost(Item item) {
    return GetCost(item.GetType());
  }

  public static int GetCost(Type itemType) {
    int cost = 0;
    var field = itemType.GetField("yieldCost", System.Reflection.BindingFlags.Static);
    if (field != null) {
      cost = (int) field.GetValue(null);
    }
    if (cost == 0) {
      UnityEngine.Debug.LogWarning(itemType + " cost 0! Defaulting to 2");
      cost = 2;
    }
    return cost;
  }

  public static int Recompute(
      Entity e,
      YieldContributionRule[] contributionRules,
      List<YieldContribution> outLatestContributions) {
    int totalYield = 0;
    foreach (var Rule in contributionRules) {
      var contribution = Rule.Invoke(e);
      totalYield += contribution.active ? contribution.bonus : 0;
      outLatestContributions.Add(contribution);
    }
    return totalYield;
  }
}
