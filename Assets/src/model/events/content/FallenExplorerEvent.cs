using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Category 15: Graveyard / Memory — loot and lore.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class FallenExplorerEvent : NarrativeEvent {
  public override string Title => "The Fallen Explorer";
  public override string Description => "Bones rest against the wall beside a weathered journal. Whatever happened here was quick.";
  public override string FlavorText => "Their pack still holds a few things worth taking.";
  public override int MinDepth => 2;
  public override int MaxDepth => 26;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Take their weapon",
        Tooltip = "Gain a weapon (1 durability remaining)",
        Effect = (c) => {
          var weapon = GetRandomWeapon();
          c.player.inventory.AddItem(weapon);
        }
      },
      new EventChoice {
        Label = "Search for supplies",
        Tooltip = "Gain 60 water and heal 4 HP",
        Effect = (c) => {
          c.player.water += 60;
          c.player.Heal(4);
        }
      },
      new EventChoice {
        Label = "Read the journal",
        Tooltip = "Gain a seed from their notes on cave flora",
        Effect = (c) => {
          var seedTypes = new Type[] {
            typeof(BerryBush), typeof(Wildwood), typeof(Thornleaf),
            typeof(StoutShrub), typeof(Faeleaf)
          };
          var seedType = seedTypes[MyRandom.Range(0, seedTypes.Length)];
          c.player.inventory.AddItem(new ItemSeed(seedType, 1));
        }
      }
    };
  }

  private Item GetRandomWeapon() {
    // Give a random weapon at low durability
    switch (MyRandom.Range(0, 3)) {
      case 0: return new ItemThickBranch();
      case 1: return new ItemStick();
      default: return new ItemThickBranch();
    }
  }
}
