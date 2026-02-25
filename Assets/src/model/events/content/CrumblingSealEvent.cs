using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 7: Environmental Shift — changes the homebase permanently.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class CrumblingSealEvent : NarrativeEvent {
  public override string Title => "The Crumbling Seal";
  public override string Description => "An ancient seal carved into the wall, cracked and humming. The air tastes different near it — wetter, more alive.";
  public override string FlavorText => "Break it, and something changes forever.";
  public override int MinDepth => 5;
  public override int MaxDepth => 22;

  public override bool CanOccur(EventContext ctx) {
    return !ctx.home.bodies.OfType<BrokenSealFragment>().Any();
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Break the seal",
        Tooltip = "2 new Water tiles appear at home permanently. A fragment remains.",
        Effect = (c) => {
          // Add Water tiles at home
          var emptyTiles = c.home.EnumerateFloor()
            .Select(p => c.home.tiles[p])
            .Where(t => t is Ground && !(t is Soil) && t.CanBeOccupied() && c.home.grasses[t.pos] == null)
            .OrderBy(t => Vector2Int.Distance(t.pos, new Vector2Int(c.home.width / 2, c.home.height / 2)))
            .Take(2)
            .ToList();
          foreach (var tile in emptyTiles) {
            c.home.Put(new Water(tile.pos));
          }
          // Place fragment at home
          var fragmentTile = c.home.EnumerateFloor()
            .Select(p => c.home.tiles[p])
            .Where(t => t is Ground && t.CanBeOccupied() && !(t is Soil))
            .FirstOrDefault();
          if (fragmentTile != null) {
            c.home.Put(new BrokenSealFragment(fragmentTile.pos));
          }
        }
      },
      new EventChoice {
        Label = "Leave it intact",
        Tooltip = "Some seals exist for a reason",
        Effect = (c) => { }
      }
    };
  }
}

/// <summary>
/// Homebase entity: remnant of the broken seal.
/// Future Echo events could reference this.
/// </summary>
[Serializable]
[ObjectInfo("colored_transparent_packed_1009",
  description: "A fragment of an ancient seal, still humming faintly.",
  flavorText: "The depths feel different since you broke it.")]
public class BrokenSealFragment : Body, IHideInSidebar {
  public BrokenSealFragment(Vector2Int pos) : base(pos) {
    this.hp = this.baseMaxHp = 100;
  }

  public override string displayName => "Seal Fragment";
}
