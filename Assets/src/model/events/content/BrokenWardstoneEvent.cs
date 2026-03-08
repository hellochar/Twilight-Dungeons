using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 19: World Scar — rare, permanent dungeon change. Max 1 per run.
/// Between-floor event, forced.
/// </summary>
[Serializable]
public class BrokenWardstoneEvent : NarrativeEvent {
  public override string Title => "The Broken Wardstone";
  public override string Description => "A massive wardstone blocks the passage, covered in fading runes. Cracks spiderweb across its surface. One blow would shatter it.";
  public override string FlavorText => "This seal held something back. Breaking it will change everything that follows.";
  public override int MinDepth => 8;
  public override int MaxDepth => 20;
  public override bool HasWalkAway => false;

  public override bool CanOccur(EventContext ctx) {
    // Only once per run
    return !ctx.home.bodies.OfType<WardstoneFragment>().Any();
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Shatter the wardstone",
        Tooltip = "2 Water tiles + 2 Soil tiles appear at home permanently. A fragment remains.",
        Effect = (c) => {
          // Add Water tiles at home
          var waterTiles = c.home.EnumerateFloor()
            .Select(p => c.home.tiles[p])
            .Where(t => t is Ground && !(t is Soil) && t.CanBeOccupied() && c.home.grasses[t.pos] == null)
            .OrderByDescending(t => Vector2Int.Distance(t.pos, new Vector2Int(c.home.width / 2, c.home.height / 2)))
            .Take(2)
            .ToList();
          foreach (var tile in waterTiles) {
            c.home.Put(new Water(tile.pos));
          }

          // Add Soil tiles at home
          var soilTiles = c.home.EnumerateFloor()
            .Select(p => c.home.tiles[p])
            .Where(t => t is Ground && !(t is Soil) && t.CanBeOccupied() && c.home.grasses[t.pos] == null)
            .OrderBy(t => Vector2Int.Distance(t.pos, new Vector2Int(c.home.width / 2, c.home.height / 2)))
            .Take(2)
            .ToList();
          foreach (var tile in soilTiles) {
            c.home.Put(new Soil(tile.pos));
          }

          // Place fragment at home
          var fragmentTile = c.home.EnumerateFloor()
            .Select(p => c.home.tiles[p])
            .Where(t => t is Ground && t.CanBeOccupied() && !(t is Soil))
            .FirstOrDefault();
          if (fragmentTile != null) {
            c.home.Put(new WardstoneFragment(fragmentTile.pos));
          }
        }
      },
      new EventChoice {
        Label = "Leave the seal intact",
        Tooltip = "The depths remain as they are",
        Effect = (c) => { }
      }
    };
  }
}

/// <summary>
/// Homebase entity: a cracked wardstone fragment.
/// Its presence marks that the world has been permanently altered.
/// Future Echo events can reference this.
/// </summary>
[Serializable]
[ObjectInfo("colored_transparent_packed_1042",
  description: "A cracked wardstone fragment, pulsing faintly.",
  flavorText: "The dungeon feels different since you broke the seal.")]
public class WardstoneFragment : Body, IHideInSidebar {
  public WardstoneFragment(Vector2Int pos) : base(pos) {
    this.hp = this.baseMaxHp = 100;
  }

  public override string displayName => "Wardstone Fragment";
}
