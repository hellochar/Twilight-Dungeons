using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 17: Garden Ritual — modify the homebase permanently.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class FertileAshEvent : NarrativeEvent {
  public override string Title => "Fertile Ash";
  public override string Description => "A smoldering pile of rich black ash. It smells of loam and growth — the remains of something that burned so its seeds could sprout.";
  public override string FlavorText => "Even destruction feeds the garden.";
  public override int MinDepth => 4;
  public override int MaxDepth => 24;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    var choices = new List<EventChoice>();

    // Option A: Add Soil tiles at home
    choices.Add(new EventChoice {
      Label = "Scatter at home",
      Tooltip = "Add 2 new planting spots to your garden",
      Effect = (c) => {
        var home = c.home;
        var emptyTiles = home.EnumerateFloor()
          .Select(p => home.tiles[p])
          .Where(t => t is Ground && !(t is Soil) && t.CanBeOccupied() && home.grasses[t.pos] == null)
          .OrderBy(t => Vector2Int.Distance(t.pos, new Vector2Int(home.width / 2, home.height / 2)))
          .Take(2)
          .ToList();
        foreach (var tile in emptyTiles) {
          home.Put(new Soil(tile.pos));
        }
      }
    });

    // Option B: Advance plant growth
    var hasPlants = ctx.home.bodies.OfType<Plant>().Any(p => p.percentGrown < 1f);
    choices.Add(new EventChoice {
      Label = "Feed to your plants",
      Tooltip = "All growing plants gain +2 growth",
      IsAvailable = (c) => c.home.bodies.OfType<Plant>().Any(p => p.percentGrown < 1f),
      UnavailableReason = "No growing plants",
      Effect = (c) => {
        foreach (var plant in c.home.bodies.OfType<Plant>().Where(p => p.percentGrown < 1f).ToList()) {
          plant.OnFloorCleared(null);
          plant.OnFloorCleared(null);
        }
      }
    });

    // Option C: Get water
    choices.Add(new EventChoice {
      Label = "Mix with water",
      Tooltip = "Gain 120 water",
      Effect = (c) => {
        c.player.water += 120;
      }
    });

    return choices;
  }
}
