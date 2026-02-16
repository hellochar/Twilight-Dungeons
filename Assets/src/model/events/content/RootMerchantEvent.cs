using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Category 9: The Vendor — trade in unusual resources.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class RootMerchantEvent : NarrativeEvent {
  public override string Title => "The Root Merchant";
  public override string Description => "A creature of tangled roots and moss rises from the earth. Its hollow eyes gleam with knowing. It gestures toward its wares — and toward you.";
  public override string FlavorText => "\"Trade. Grow. Trade.\"";
  public override int MinDepth => 3;
  public override int MaxDepth => 25;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    var choices = new List<EventChoice>();

    // Trade A: Give 2 seeds → get a rare seed
    var seedCount = ctx.AllPlayerItems().OfType<ItemSeed>().Sum(s => s.stacks);
    choices.Add(new EventChoice {
      Label = "Offer two seeds",
      Tooltip = "Receive a rare seed in return",
      IsAvailable = (c) => c.AllPlayerItems().OfType<ItemSeed>().Sum(s => s.stacks) >= 2,
      UnavailableReason = "Need at least 2 seeds",
      Effect = (c) => {
        // Remove 2 seeds
        int toRemove = 2;
        foreach (var seed in c.AllPlayerItems().OfType<ItemSeed>().ToList()) {
          while (toRemove > 0 && seed.stacks > 0) {
            seed.stacks--;
            toRemove--;
          }
          if (seed.stacks <= 0) {
            seed.Destroy();
          }
          if (toRemove <= 0) break;
        }
        // Give a rare seed
        var rareTypes = new Type[] {
          typeof(Kingshroom), typeof(Weirdwood), typeof(ChangErsWillow), typeof(Frizzlefen)
        };
        c.player.inventory.AddItem(new ItemSeed(rareTypes[MyRandom.Range(0, rareTypes.Length)], 1));
      }
    });

    // Trade B: Give 200 water → advance all home plants
    choices.Add(new EventChoice {
      Label = "Offer 200 water",
      Tooltip = "All growing plants at home gain +2 growth",
      IsAvailable = (c) => c.player.water >= 200,
      UnavailableReason = "Not enough water",
      Effect = (c) => {
        c.player.water -= 200;
        foreach (var plant in c.home.bodies.OfType<Plant>().Where(p => p.percentGrown < 1f).ToList()) {
          plant.OnFloorCleared(null);
          plant.OnFloorCleared(null);
        }
      }
    });

    // Trade C: Give any weapon → get 2 random seeds
    choices.Add(new EventChoice {
      Label = "Offer a weapon",
      Tooltip = "Receive 2 random seeds",
      IsAvailable = (c) => c.AllPlayerItems().Any(i => i is IWeapon && !(i is ItemHands)),
      UnavailableReason = "No weapon to trade",
      Effect = (c) => {
        var weapon = c.AllPlayerItems().FirstOrDefault(i => i is IWeapon && !(i is ItemHands));
        weapon?.Destroy();
        var seedTypes = new Type[] {
          typeof(BerryBush), typeof(Wildwood), typeof(Thornleaf),
          typeof(StoutShrub), typeof(Broodpuff)
        };
        c.player.inventory.AddItem(new ItemSeed(seedTypes[MyRandom.Range(0, seedTypes.Length)], 1));
        c.player.inventory.AddItem(new ItemSeed(seedTypes[MyRandom.Range(0, seedTypes.Length)], 1));
      }
    });

    return choices;
  }
}
