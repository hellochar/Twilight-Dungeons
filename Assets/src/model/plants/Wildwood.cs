using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Wildwood : Plant {
  [Serializable]
  class Mature : PlantStage {
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      // harvestOptions.Add(new Inventory());
      // harvestOptions.Add(new Inventory());
      // harvestOptions.Add(new Inventory());
      // harvestOptions.Add(new Inventory(
      //   new ItemSeed(typeof(Wildwood), 2),
      //   new ItemStick()
      // ));
      // harvestOptions.Add(new Inventory(
      //   new ItemSeed(typeof(Wildwood)),
      //   new ItemWildwoodLeaf(3),
      //   new ItemWildwoodWreath()
      // ));
      // harvestOptions.Add(new Inventory(
      //   new ItemWildwoodRod()
      // ));
    }
    public static Dictionary<Type, int> MakeOfferings(params Type[] types) {
      Dictionary<Type, int> offerings = new Dictionary<Type, int>();
      foreach(Type t in types) {
        int cost = YieldContributionUtils.GetCost(t);
        offerings[t] = cost;
      }
      return offerings;
    }

    static Dictionary<Type, int> offerings = MakeOfferings(typeof(ItemWildwoodLeaf), typeof(ItemWildwoodWreath), typeof(ItemStick), typeof(ItemWildwoodRod));

    internal override void RestockHarvests(int yield) {
      // pick a random inventory
      // Inventory bucket = Util.RandomPick(harvestOptions);
      harvestOptions.Clear();

      // find the items that you can at least make one of
      // var choices = offerings.Keys.Where(itemType => offerings[itemType] <= yield);
      var choices = offerings.Keys;
      foreach(var itemType in choices) {
        // var itemType = Util.RandomPick(choices);
        var constructor = itemType.GetConstructor(new Type[0]);
        var item = (Item) constructor.Invoke(new object[0]);
        // set number of stacks to yield
        // item.stacks = (int)(yield / offerings[itemType]);
        harvestOptions.Add(new Inventory(item));
      }
      // Debug.Log($"Yield {yield}, choices: {String.Join(", ", choices)}, chose {item} on bucket {harvestOptions.IndexOf(bucket)}.");
      // if (yield >= 5) {
      //   var numOrganicMatter = yield / 5;
      //   var organicMatters = Enumerable.Range(0, numOrganicMatter).Select(i => new ItemOrganicMatter());
      //   harvestOptions.Add(new Inventory(organicMatters.ToArray()));
      // }

      harvestOptions.Add(new Inventory(new ItemSeed(typeof(Wildwood), 1)));
    }
  }

  public Wildwood(Vector2Int pos) : base(pos, new Seed()) {
    stage.NextStage = new Mature();
  }

  public static YieldContribution AllAreaAdjacentTilesEmptyYieldContribution(Entity p) {
    // var emptySpaces = p.floor.GetAdjacentTiles(p.pos).Where(t => t.CanBeOccupied() || t.body is Player);
    var occupiedSpaces = p.AreaAdjacentTiles().Where(t => {
      if (t.body is Player) {
        return false;
      }
      if (t.grass != null) {
        return true;
      }
      if (t.body != null) {
        return true;
      }
      return false;
    });
    return new YieldContribution {
      active = occupiedSpaces.Count() < 2,
      bonus = 10,
      description = $"Only one adjacent Tile is occupied (there are {occupiedSpaces.Count()})."
    };
  }

  public static YieldContribution NextToChasmYieldContribution(Entity p) {
    var numChasms = p.AreaAdjacentTiles().OfType<Chasm>().Count();
    return new YieldContribution {
      active = numChasms > 0,
      bonus = numChasms * 2,
      description = $"+2 for each nearby chasm."
    };
  }

  public static YieldContributionRule[] MyContributionRules => BaseContributionRules.Concat(
    new YieldContributionRule[] {
      AllAreaAdjacentTilesEmptyYieldContribution,
      NextToChasmYieldContribution,
    }
  ).ToArray();
  public override YieldContributionRule[] contributionRules => MyContributionRules;
}

[Serializable]
[ObjectInfo("colored_transparent_packed_179", "It seems to bend and twist on its own, as if it were wielding you!")]
internal class ItemWildwoodRod : EquippableItem, IWeapon, IActionPerformedHandler {
  public static int yieldCost = 6;
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  public (int, int) AttackSpread => (3, 5);

  public override int stacksMax => int.MaxValue;
  // public override bool disjoint => true;

  internal override string GetStats() => "Automatically attack an adjacent enemy when you move.";

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.MOVE) {
      var target = player.floor.AdjacentActors(player.pos).Where(a => a.faction == Faction.Enemy).FirstOrDefault();
      if (target != null) {
        player.Perform(new AttackBaseAction(player, target));
      }
    }
  }
}