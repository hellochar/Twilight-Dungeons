using System;
using System.Collections.Generic;

/// <summary>
/// Category 4: The Fork (Path Choice) — choose your immediate benefit.
/// Between-floor event, forced.
/// </summary>
[Serializable]
public class BranchingTunnelEvent : NarrativeEvent {
  public override string Title => "The Branching Tunnel";
  public override string Description => "The passage splits into three. Each path hums with a different resonance. You can only take one.";
  public override string FlavorText => "Choose your path.";
  public override int MinDepth => 3;
  public override int MaxDepth => 24;
  public override bool HasWalkAway => false;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "The flooded path",
        Tooltip = "Gain 100 water, heal 4 HP",
        Effect = (c) => {
          c.player.water += 100;
          c.player.Heal(4);
        }
      },
      new EventChoice {
        Label = "The overgrown path",
        Tooltip = "Gain a random seed",
        Effect = (c) => {
          var seedTypes = new Type[] {
            typeof(BerryBush), typeof(Wildwood), typeof(Thornleaf),
            typeof(StoutShrub), typeof(Broodpuff), typeof(Faeleaf)
          };
          c.player.inventory.AddItem(new ItemSeed(seedTypes[MyRandom.Range(0, seedTypes.Length)], 1));
        }
      },
      new EventChoice {
        Label = "The silent path",
        Tooltip = "Gain 1 heart permanently (+4 max HP)",
        Effect = (c) => {
          c.player.baseMaxHp += 4;
        }
      }
    };
  }
}
