using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 3: Body Mutation — permanent status added to the player.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class FungalPoolEvent : NarrativeEvent {
  public override string Title => "The Fungal Pool";
  public override string Description => "Luminous spores drift across a shallow pool. The water glows with an unearthly light. You can feel it pulling at something inside you.";
  public override string FlavorText => "\"Step in, and you will never be the same.\"";
  public override int MinDepth => 5;
  public override int MaxDepth => 20;

  public override bool CanOccur(EventContext ctx) {
    // Don't offer if already mutated
    return !ctx.player.statuses.list.OfType<FungalMarkStatus>().Any();
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Step into the pool",
        Tooltip = "Permanent: take 1 less attack damage. Gain Fungal Mark.",
        Effect = (c) => {
          c.player.statuses.Add(new FungalMarkStatus());
          // Place a remnant at home to track this choice
          var homeTile = c.home.EnumerateFloor()
            .Select(p => c.home.tiles[p])
            .Where(t => t is Ground && t.CanBeOccupied() && !(t is Soil))
            .FirstOrDefault();
          if (homeTile != null) {
            c.home.Put(new FungalPoolRemnant(homeTile.pos));
          }
        }
      },
      new EventChoice {
        Label = "Walk around",
        Tooltip = "Some changes can't be undone",
        Effect = (c) => { }
      }
    };
  }
}

/// <summary>
/// Permanent status: reduces attack damage taken by 1.
/// Not a debuff, so it persists across floors.
/// </summary>
[Serializable]
public class FungalMarkStatus : Status, IAttackDamageTakenModifier {
  public override string displayName => "Fungal Mark";
  public override bool isDebuff => false;
  public int reductionAmount = 1;

  internal override string GetStats() => $"Take {reductionAmount} less attack damage.";

  public int Modify(int input) {
    return Math.Max(0, input - reductionAmount);
  }

  public override void HandleFloorChanged(Floor newFloor, Floor oldFloor) {
    // Do NOT remove — this is permanent
  }

  public override void Step(Actor actor) {
    // Permanent — no decay
  }
}

/// <summary>
/// Homebase entity tracking that the player has been mutated by the Fungal Pool.
/// Used by Echo events (MarkedStoneEvent) to offer follow-up choices.
/// </summary>
[Serializable]
[ObjectInfo("colored_transparent_packed_571",
  description: "A fragment of glowing fungal matter, pulsing faintly.",
  flavorText: "It reacts to your changed body.")]
public class FungalPoolRemnant : Body, IHideInSidebar {
  public FungalPoolRemnant(Vector2Int pos) : base(pos) {
    this.hp = this.baseMaxHp = 100;
  }

  public override string displayName => "Fungal Remnant";
}
