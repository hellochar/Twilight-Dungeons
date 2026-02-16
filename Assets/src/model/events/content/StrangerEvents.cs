using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 20: The Stranger — recurring NPC, relationship tracked via homebase entity.
/// Encounter 1: first meeting, around depth 4.
/// </summary>
[Serializable]
public class StrangerEvent1 : NarrativeEvent {
  public override string Title => "The Stranger";
  public override string Description => "A figure in a tattered cloak slumps against the wall. They're hurt — badly. They look up at you with weary eyes.";
  public override string FlavorText => "\"Please...\"";
  public override int MinDepth => 3;
  public override int MaxDepth => 8;

  public override bool CanOccur(EventContext ctx) {
    // Only if we haven't met them yet
    return !ctx.home.bodies.OfType<StrangerNPC>().Any();
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Help them",
        Tooltip = "Lose 4 HP. They'll remember this.",
        Effect = (c) => {
          c.player.TakeDamage(4, null);
          c.model.DrainEventQueue();
          // Place the Stranger at home
          var homePos = FindEmptyHomePos(c.home);
          var stranger = new StrangerNPC(homePos);
          stranger.timesHelped = 1;
          c.home.Put(stranger);
        }
      },
      new EventChoice {
        Label = "Walk past",
        Tooltip = "You have your own problems.",
        Effect = (c) => { }
      }
    };
  }

  private Vector2Int FindEmptyHomePos(Floor home) {
    // Find a ground tile near the center that isn't occupied
    var center = new Vector2Int(home.width / 2, home.height / 2);
    var candidates = home.BreadthFirstSearch(center, t => true)
      .Where(t => t is Ground && t.CanBeOccupied() && !(t is Soil))
      .Take(10);
    var tile = candidates.FirstOrDefault();
    return tile?.pos ?? center;
  }
}

/// <summary>
/// Encounter 2: second meeting, around depth 10-14.
/// </summary>
[Serializable]
public class StrangerEvent2 : NarrativeEvent {
  public override string Title => "The Stranger";
  public override int MinDepth => 10;
  public override int MaxDepth => 16;

  public override string Description {
    get {
      var stranger = GameModel.main?.home?.bodies.OfType<StrangerNPC>().FirstOrDefault();
      if (stranger != null) {
        return "The cloaked figure steps from the shadows. They recognize you — there's warmth in their eyes. \"I found something. For you.\"";
      }
      return "A familiar figure blocks the passage. They glance at you with cold recognition. \"You left me to die. The passage demands payment.\"";
    }
  }
  public override string FlavorText => "";

  public override bool HasWalkAway {
    get {
      var stranger = GameModel.main?.home?.bodies.OfType<StrangerNPC>().FirstOrDefault();
      return stranger != null; // Can only walk away if you helped before
    }
  }

  public override bool CanOccur(EventContext ctx) {
    // Always can occur at this depth — behavior branches on whether Stranger is at home
    return true;
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    var stranger = ctx.home.bodies.OfType<StrangerNPC>().FirstOrDefault();

    if (stranger != null) {
      // They helped before — reward them
      return new List<EventChoice> {
        new EventChoice {
          Label = "Accept their gift",
          Tooltip = "Receive a rare seed",
          Effect = (c) => {
            var seedTypes = new Type[] {
              typeof(Kingshroom), typeof(Weirdwood), typeof(ChangErsWillow), typeof(Frizzlefen)
            };
            var seedType = seedTypes[MyRandom.Range(0, seedTypes.Length)];
            c.player.inventory.AddItem(new ItemSeed(seedType, 1));
            stranger.timesHelped++;
          }
        }
      };
    } else {
      // They didn't help — forced payment
      return new List<EventChoice> {
        new EventChoice {
          Label = "Pay 4 HP to pass",
          IsAvailable = (c) => c.player.hp > 4,
          UnavailableReason = "Too dangerous",
          Effect = (c) => {
            c.player.TakeDamage(4, null);
            c.model.DrainEventQueue();
          }
        },
        new EventChoice {
          Label = "Pay 80 water to pass",
          IsAvailable = (c) => c.player.water >= 80,
          UnavailableReason = "Not enough water",
          Effect = (c) => {
            c.player.water -= 80;
          }
        }
      };
    }
  }
}
