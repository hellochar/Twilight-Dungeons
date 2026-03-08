using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 13: Curse/Gift Duality — gain something both good and bad.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class ThornbloodPactEvent : NarrativeEvent {
  public override string Title => "Thornblood Pact";
  public override string Description => "Thorns grow from a cracked altar, dripping with dark sap. They pulse with a strange vitality.";
  public override string FlavorText => "\"Touch them, and gain their protection — and their thirst.\"";
  public override int MinDepth => 6;
  public override int MaxDepth => 22;

  public override bool CanOccur(EventContext ctx) {
    return !ctx.player.statuses.list.OfType<ThornbloodStatus>().Any();
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Accept the pact",
        Tooltip = "Permanent: take 1 less attack damage, but lose 8 water whenever you take damage",
        Effect = (c) => {
          c.player.statuses.Add(new ThornbloodStatus());
          // Place altar at home
          var homeTile = c.home.EnumerateFloor()
            .Select(p => c.home.tiles[p])
            .Where(t => t is Ground && t.CanBeOccupied() && !(t is Soil))
            .FirstOrDefault();
          if (homeTile != null) {
            c.home.Put(new ThornbloodAltar(homeTile.pos));
          }
        }
      },
      new EventChoice {
        Label = "Refuse",
        Tooltip = "Leave the thorns undisturbed",
        Effect = (c) => { }
      }
    };
  }
}

/// <summary>
/// Permanent dual status: reduces attack damage taken by 1, but drains water on any damage.
/// </summary>
[Serializable]
public class ThornbloodStatus : Status, IAttackDamageTakenModifier, ITakeAnyDamageHandler {
  public override string displayName => "Thornblood";
  public override bool isDebuff => false;

  internal override string GetStats() => "Take 1 less attack damage.\nLose 8 water whenever you take damage.";

  public int Modify(int input) {
    return Math.Max(0, input - 1);
  }

  public void HandleTakeAnyDamage(int damage) {
    if (list?.actor is Player player && damage > 0) {
      player.water = Math.Max(0, player.water - 8);
    }
  }

  public override void HandleFloorChanged(Floor newFloor, Floor oldFloor) {
    // Permanent — do not remove
  }

  public override void Step(Actor actor) {
    // Permanent — no decay
  }
}

/// <summary>
/// Homebase entity marking the Thornblood Pact.
/// </summary>
[Serializable]
[ObjectInfo("colored_transparent_packed_496",
  description: "A thorned altar, dark sap still dripping.",
  flavorText: "Its protection comes at a cost.")]
public class ThornbloodAltar : Body, IHideInSidebar {
  public ThornbloodAltar(Vector2Int pos) : base(pos) {
    this.hp = this.baseMaxHp = 100;
  }

  public override string displayName => "Thornblood Altar";
}
