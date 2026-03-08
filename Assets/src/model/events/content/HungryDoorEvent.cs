using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Category 18: The Toll — forced, no walk-away.
/// Between-floor event.
/// </summary>
[Serializable]
public class HungryDoorEvent : NarrativeEvent {
  public override string Title => "The Hungry Door";
  public override string Description => "The passage narrows to a mouth of stone. It won't let you pass without tribute.";
  public override string FlavorText => "Somewhere in the dark, something chews.";
  public override int MinDepth => 5;
  public override int MaxDepth => 24;
  public override bool HasWalkAway => false;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Feed it an edible item",
        Tooltip = "The door accepts organic offerings",
        IsAvailable = (c) => c.AllPlayerItems().Any(i => i is IEdible),
        UnavailableReason = "No edible items",
        Effect = (c) => {
          var edible = c.AllPlayerItems().FirstOrDefault(i => i is IEdible);
          edible?.Destroy();
        }
      },
      new EventChoice {
        Label = "Sacrifice 4 HP",
        Tooltip = "The door bites as you squeeze through",
        IsAvailable = (c) => c.player.hp > 4,
        UnavailableReason = "Too dangerous",
        Effect = (c) => {
          c.player.TakeDamage(4, null);
          c.model.DrainEventQueue();
        }
      },
      new EventChoice {
        Label = "Offer 80 water",
        Tooltip = "Pour water into the cracks",
        IsAvailable = (c) => c.player.water >= 80,
        UnavailableReason = "Not enough water",
        Effect = (c) => {
          c.player.water -= 80;
        }
      }
    };
  }
}
