using System;
using System.Collections.Generic;

/// <summary>
/// Category 16: The Wager — controlled gambling with known odds.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class StoneDiceEvent : NarrativeEvent {
  public override string Title => "The Stone Dice";
  public override string Description => "Two carved dice rest on a flat altar stone. Ancient marks surround them — winners and losers alike left their tally.";
  public override string FlavorText => "Roll 2d6. Sum of 7 or higher wins (~58% chance).";
  public override int MinDepth => 3;
  public override int MaxDepth => 25;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Roll the dice",
        Tooltip = "Win: gain a seed. Lose: lose 1 heart permanently.",
        IsAvailable = (c) => c.player.baseMaxHp > 4,
        UnavailableReason = "Too fragile to risk",
        Effect = (c) => {
          int die1 = MyRandom.Range(1, 7);
          int die2 = MyRandom.Range(1, 7);
          int sum = die1 + die2;

          if (sum >= 7) {
            // Win!
            var seedTypes = new Type[] {
              typeof(Kingshroom), typeof(Weirdwood), typeof(ChangErsWillow),
              typeof(Frizzlefen), typeof(Broodpuff)
            };
            var seedType = seedTypes[MyRandom.Range(0, seedTypes.Length)];
            c.player.inventory.AddItem(new ItemSeed(seedType, 1));
          } else {
            // Lose
            c.player.baseMaxHp -= 4;
            if (c.player.hp > c.player.maxHp) {
              c.player.SetHPDirect(c.player.maxHp);
            }
          }
        }
      },
      new EventChoice {
        Label = "Roll cautiously",
        Tooltip = "Win: gain 80 water. Lose: lose 40 water.",
        Effect = (c) => {
          int die1 = MyRandom.Range(1, 7);
          int die2 = MyRandom.Range(1, 7);
          int sum = die1 + die2;

          if (sum >= 7) {
            c.player.water += 80;
          } else {
            c.player.water = Math.Max(0, c.player.water - 40);
          }
        }
      }
    };
  }
}
