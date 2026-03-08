using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Category 14: Resource Wellspring — abundance with a catch.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class HealingSpringEvent : NarrativeEvent {
  public override string Title => "Healing Spring";
  public override string Description => "Crystal-clear water fills a natural basin in the stone. Its surface shimmers with an unnatural purity.";
  public override string FlavorText => "The water calls to you, promising renewal — and erasure.";
  public override int MinDepth => 3;
  public override int MaxDepth => 26;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Drink deeply",
        Tooltip = "Heal to full, gain 100 water. All statuses removed.",
        Effect = (c) => {
          c.player.Heal(c.player.maxHp);
          c.player.water += 100;
          // Remove all statuses — the spring purifies everything
          foreach (var status in c.player.statuses.list.ToList()) {
            status.Remove();
          }
          c.model.DrainEventQueue();
        }
      },
      new EventChoice {
        Label = "Fill your reserves only",
        Tooltip = "Gain 60 water, keep your statuses",
        Effect = (c) => {
          c.player.water += 60;
        }
      }
    };
  }
}
