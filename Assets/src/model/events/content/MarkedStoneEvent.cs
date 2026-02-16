using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Category 6: Echo / Callback — references earlier Fungal Pool choice.
/// Between-floor event. Only appears if FungalPoolRemnant exists at home.
/// </summary>
[Serializable]
public class MarkedStoneEvent : NarrativeEvent {
  public override string Title => "The Marked Stone";
  public override string Description => "A flat stone in the passage pulses with dim light. As you approach, it reacts — to something inside you.";
  public override string FlavorText => "The stone knows what the pool made you.";
  public override int MinDepth => 10;
  public override int MaxDepth => 24;

  public override bool CanOccur(EventContext ctx) {
    return ctx.home.bodies.OfType<FungalPoolRemnant>().Any();
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    var fungalMark = ctx.player.statuses.list.OfType<FungalMarkStatus>().FirstOrDefault();

    var choices = new List<EventChoice>();

    if (fungalMark != null) {
      // Deepen the transformation
      choices.Add(new EventChoice {
        Label = "Deepen the transformation",
        Tooltip = "Take 2 less attack damage instead of 1",
        IsAvailable = (c) => c.player.statuses.list.OfType<FungalMarkStatus>().Any(s => s.reductionAmount < 2),
        UnavailableReason = "Already at maximum depth",
        Effect = (c) => {
          var mark = c.player.statuses.list.OfType<FungalMarkStatus>().FirstOrDefault();
          if (mark != null) {
            mark.reductionAmount = 2;
          }
        }
      });

      // Purify — remove the transformation
      choices.Add(new EventChoice {
        Label = "Purify yourself",
        Tooltip = "Remove Fungal Mark. Gain 1 heart permanently (+4 max HP).",
        Effect = (c) => {
          var mark = c.player.statuses.list.OfType<FungalMarkStatus>().FirstOrDefault();
          mark?.Remove();
          c.model.DrainEventQueue();
          c.player.baseMaxHp += 4;
          // Remove remnant from home
          var remnant = c.home.bodies.OfType<FungalPoolRemnant>().FirstOrDefault();
          if (remnant != null) {
            c.home.Remove(remnant);
          }
        }
      });
    } else {
      // They had the remnant but somehow lost the status — offer a second chance
      choices.Add(new EventChoice {
        Label = "Touch the stone",
        Tooltip = "Gain Fungal Mark: take 1 less attack damage permanently",
        Effect = (c) => {
          c.player.statuses.Add(new FungalMarkStatus());
        }
      });
    }

    return choices;
  }
}
