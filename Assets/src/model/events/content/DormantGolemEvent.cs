using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 11: Companion — temporary ally entity.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class DormantGolemEvent : NarrativeEvent {
  public override string Title => "The Dormant Golem";
  public override string Description => "A small stone golem lies half-buried in rubble. Faint runes still glow along its arms. It looks like it could be reactivated — or taken apart.";
  public override string FlavorText => "Ancient craft, waiting to serve once more.";
  public override int MinDepth => 6;
  public override int MaxDepth => 24;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Reactivate it",
        Tooltip = "A Golem ally fights by your side on this floor",
        Effect = (c) => {
          var floor = GameModel.main.currentFloor;
          var allyTile = floor.GetAdjacentTiles(c.player.pos)
            .Where(t => t.CanBeOccupied())
            .FirstOrDefault();
          if (allyTile == null) {
            allyTile = floor.BreadthFirstSearch(c.player.pos, t => true)
              .Where(t => t.CanBeOccupied() && t.pos != c.player.pos)
              .FirstOrDefault();
          }
          if (allyTile != null) {
            var ally = new Golem(allyTile.pos);
            ally.faction = Faction.Ally;
            ally.SetAI(new CharmAI(ally));
            floor.Put(ally);
          }
        }
      },
      new EventChoice {
        Label = "Harvest for parts",
        Tooltip = "Gain 80 water from its core",
        Effect = (c) => {
          c.player.water += 80;
        }
      },
      new EventChoice {
        Label = "Bring it home",
        Tooltip = "A stone guardian appears at your garden",
        Effect = (c) => {
          var homeTile = c.home.EnumerateFloor()
            .Select(p => c.home.tiles[p])
            .Where(t => t is Ground && t.CanBeOccupied() && !(t is Soil))
            .FirstOrDefault();
          if (homeTile != null) {
            c.home.Put(new GolemGuardian(homeTile.pos));
          }
        }
      }
    };
  }
}

/// <summary>
/// Homebase entity: a dormant golem guardian resting in the garden.
/// </summary>
[Serializable]
[ObjectInfo("colored_transparent_packed_982",
  description: "A small stone golem, resting quietly.",
  flavorText: "It watches over the garden with ancient patience.")]
public class GolemGuardian : Body, IHideInSidebar {
  public GolemGuardian(Vector2Int pos) : base(pos) {
    this.hp = this.baseMaxHp = 100;
  }

  public override string displayName => "Golem Guardian";
}
