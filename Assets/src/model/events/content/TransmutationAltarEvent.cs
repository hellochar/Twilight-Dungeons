using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Category 1: Transmutation — convert between resource types.
/// Placed as an in-world EventBody on cave floors.
/// </summary>
[Serializable]
public class TransmutationAltarEvent : NarrativeEvent {
  public override string Title => "Transmutation Altar";
  public override string Description => "A stone altar pulses with faint light. Channels are carved into its surface — one filled with water, one with a dark residue.";
  public override string FlavorText => "\"Place something upon the stone and see what it becomes.\"";
  public override int MinDepth => 2;
  public override int MaxDepth => 25;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    var choices = new List<EventChoice>();

    choices.Add(new EventChoice {
      Label = "Offer 80 Water",
      Tooltip = "Receive a random seed",
      IsAvailable = (c) => c.player.water >= 80,
      UnavailableReason = "Not enough water",
      Effect = (c) => {
        c.player.water -= 80;
        var seedTypes = new Type[] {
          typeof(BerryBush), typeof(Wildwood), typeof(Thornleaf),
          typeof(StoutShrub), typeof(Faeleaf)
        };
        var seedType = seedTypes[MyRandom.Range(0, seedTypes.Length)];
        c.player.inventory.AddItem(new ItemSeed(seedType, 1));
      }
    });

    // Offer an edible item for water
    var hasEdible = ctx.AllPlayerItems().Any(i => i is IEdible);
    choices.Add(new EventChoice {
      Label = "Offer an edible item",
      Tooltip = "Receive 50 water",
      IsAvailable = (c) => c.AllPlayerItems().Any(i => i is IEdible),
      UnavailableReason = "No edible items",
      Effect = (c) => {
        var edible = c.AllPlayerItems().FirstOrDefault(i => i is IEdible);
        if (edible != null) {
          edible.Destroy();
          c.player.water += 50;
        }
      }
    });

    return choices;
  }
}
