using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 10: Resonance (Build-Synergy) — events that check items and unlock bonus choices.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class WhisperingMossEvent : NarrativeEvent {
  public override string Title => "Whispering Moss";
  public override string Description => "A patch of luminous moss clings to the wall, pulsing gently. It seems to react to your presence.";
  public override string FlavorText => "The moss hums a low, familiar note.";
  public override int MinDepth => 4;
  public override int MaxDepth => 24;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    var choices = new List<EventChoice>();

    // Base choice: always available
    choices.Add(new EventChoice {
      Label = "Touch the moss",
      Tooltip = "Heal 4 HP",
      Effect = (c) => {
        c.player.Heal(4);
      }
    });

    // Bonus: only if player has broodpuff-related items
    choices.Add(new EventChoice {
      Label = "Feed it a Broodpuff item",
      Tooltip = "A mature Broodpuff appears at your garden",
      IsAvailable = (c) => c.AllPlayerItems().Any(i => i is ItemLeecher || i is ItemBroodleaf),
      UnavailableReason = "Requires a Broodpuff item",
      Effect = (c) => {
        // Consume the broodpuff item
        var item = c.AllPlayerItems().FirstOrDefault(i => i is ItemLeecher || i is ItemBroodleaf);
        item?.Destroy();
        // Place a mature Broodpuff at home
        var homeTile = c.home.EnumerateFloor()
          .Select(p => c.home.tiles[p])
          .Where(t => t is Soil && t.CanBeOccupied())
          .FirstOrDefault();
        if (homeTile == null) {
          homeTile = c.home.EnumerateFloor()
            .Select(p => c.home.tiles[p])
            .Where(t => t is Ground && t.CanBeOccupied())
            .FirstOrDefault();
        }
        if (homeTile != null) {
          var plant = new Broodpuff(homeTile.pos);
          plant.GoNextStage();
          plant.GoNextStage();
          c.home.Put(plant);
        }
      }
    });

    // Bonus: fungal items synergy
    choices.Add(new EventChoice {
      Label = "Press a mushroom into the moss",
      Tooltip = "Gain 80 water and a Kingshroom seed",
      IsAvailable = (c) => c.AllPlayerItems().Any(i => i is ItemMushroom),
      UnavailableReason = "Requires a mushroom",
      Effect = (c) => {
        var mushroom = c.AllPlayerItems().FirstOrDefault(i => i is ItemMushroom);
        mushroom?.Destroy();
        c.player.water += 80;
        c.player.inventory.AddItem(new ItemSeed(typeof(Kingshroom), 1));
      }
    });

    return choices;
  }
}
