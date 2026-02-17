using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 5: Parasite / Symbiote — something that changes your combat.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class ClingingVineEvent : NarrativeEvent {
  public override string Title => "The Clinging Vine";
  public override string Description => "A small vine writhes from a crack in the stone, reaching toward you. It pulses with life — hungry, but not hostile.";
  public override string FlavorText => "It wants to grow on you.";
  public override int MinDepth => 4;
  public override int MaxDepth => 22;

  public override bool CanOccur(EventContext ctx) {
    return !ctx.player.statuses.list.OfType<ClingingVineStatus>().Any();
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Let it attach",
        Tooltip = "Permanent: +1 attack damage. After 5 floors, harvest for a seed at home.",
        Effect = (c) => {
          c.player.statuses.Add(new ClingingVineStatus());
        }
      },
      new EventChoice {
        Label = "Harvest it now",
        Tooltip = "Gain a common seed immediately",
        Effect = (c) => {
          var seedTypes = new Type[] {
            typeof(BerryBush), typeof(Wildwood), typeof(Thornleaf)
          };
          c.player.inventory.AddItem(new ItemSeed(seedTypes[MyRandom.Range(0, seedTypes.Length)], 1));
        }
      },
      new EventChoice {
        Label = "Leave it",
        Tooltip = "Some things are better left alone",
        Effect = (c) => { }
      }
    };
  }
}

/// <summary>
/// Permanent status that boosts attack damage and matures over floors.
/// After 5 floors, grants a rare seed on next floor clear (then removes itself).
/// </summary>
[Serializable]
public class ClingingVineStatus : Status, IAttackDamageModifier, IFloorClearedHandler {
  public override string displayName => "Clinging Vine";
  public override bool isDebuff => false;
  public int floorsAttached = 0;
  private bool matured = false;

  internal override string GetStats() {
    if (matured) {
      return "+1 attack damage.\nThe vine is mature — clear a floor to harvest a rare seed.";
    }
    return $"+1 attack damage.\nFloors until mature: {Math.Max(0, 5 - floorsAttached)}";
  }

  public int Modify(int input) {
    return input + 1;
  }

  public void HandleFloorCleared(Floor floor) {
    floorsAttached++;
    if (matured) {
      // Harvest — give a rare seed and remove self
      var player = list?.actor as Player;
      if (player != null) {
        var rareTypes = new Type[] {
          typeof(Kingshroom), typeof(Weirdwood), typeof(ChangErsWillow), typeof(Frizzlefen)
        };
        player.inventory.AddItem(new ItemSeed(rareTypes[MyRandom.Range(0, rareTypes.Length)], 1));
      }
      Remove();
    } else if (floorsAttached >= 5) {
      matured = true;
    }
  }

  public override void HandleFloorChanged(Floor newFloor, Floor oldFloor) {
    // Permanent — do not remove on floor change
  }

  public override void Step(Actor actor) {
    // No per-turn effect
  }
}
