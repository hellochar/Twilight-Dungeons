using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 12: Transformation Gauntlet — rapid sequence of chained choices.
/// Between-floor event, forced.
/// </summary>
[Serializable]
public class MirrorCorridorEvent : NarrativeEvent {
  public override string Title => "The Mirror Corridor";
  public override string Description => "Three mirrors line the narrow passage, each shimmering with a different reflection. You must face each one.";
  public override string FlavorText => "Every mirror holds a choice.";
  public override int MinDepth => 6;
  public override int MaxDepth => 24;
  public override bool HasWalkAway => false;

  /// <summary>
  /// Override Present to chain 3 popups rather than using the standard single popup.
  /// </summary>
  public override void Present(EventContext ctx) {
    PresentMirror1(ctx);
  }

  private void PresentMirror1(EventContext ctx) {
    var buttons = new List<(string, Action)> {
      ("Gain 1 heart (+4 max HP)", () => {
        ctx.player.baseMaxHp += 4;
        PresentMirror2(ctx);
      }),
      ("Gain a random seed", () => {
        var seedTypes = new Type[] {
          typeof(BerryBush), typeof(Wildwood), typeof(Thornleaf),
          typeof(StoutShrub), typeof(Broodpuff)
        };
        ctx.player.inventory.AddItem(new ItemSeed(seedTypes[MyRandom.Range(0, seedTypes.Length)], 1));
        PresentMirror2(ctx);
      })
    };

    Popups.CreateStandard(
      title: "First Mirror",
      category: "Event",
      info: "Your reflection shows two possible futures.",
      flavor: "\"Body or garden?\"",
      buttons: buttons
    );
  }

  private void PresentMirror2(EventContext ctx) {
    var buttons = new List<(string, Action)> {
      ("Trade water for soil", () => {
        ctx.player.water = Math.Max(0, ctx.player.water - 80);
        // Add 2 Soil tiles at home
        var emptyTiles = ctx.home.EnumerateFloor()
          .Select(p => ctx.home.tiles[p])
          .Where(t => t is Ground && !(t is Soil) && t.CanBeOccupied() && ctx.home.grasses[t.pos] == null)
          .OrderBy(t => Vector2Int.Distance(t.pos, new Vector2Int(ctx.home.width / 2, ctx.home.height / 2)))
          .Take(2)
          .ToList();
        foreach (var tile in emptyTiles) {
          ctx.home.Put(new Soil(tile.pos));
        }
        PresentMirror3(ctx);
      }),
      ("Keep your water", () => {
        PresentMirror3(ctx);
      })
    };

    Popups.CreateStandard(
      title: "Second Mirror",
      category: "Event",
      info: "Your reflection holds out empty hands — and shows a lush garden behind them.",
      flavor: "\"Sacrifice for growth, or hold what you have?\"",
      buttons: buttons
    );
  }

  private void PresentMirror3(EventContext ctx) {
    var buttons = new List<(string, Action)> {
      ("Heal to full", () => {
        ctx.player.Heal(ctx.player.maxHp);
        ctx.afterEvent?.Invoke();
      }),
      ("Gain a rare seed", () => {
        var rareTypes = new Type[] {
          typeof(Kingshroom), typeof(Weirdwood), typeof(ChangErsWillow), typeof(Frizzlefen)
        };
        ctx.player.inventory.AddItem(new ItemSeed(rareTypes[MyRandom.Range(0, rareTypes.Length)], 1));
        ctx.afterEvent?.Invoke();
      })
    };

    Popups.CreateStandard(
      title: "Third Mirror",
      category: "Event",
      info: "Your reflection is battered but standing. The final mirror offers mercy — or ambition.",
      flavor: "\"Now or later?\"",
      buttons: buttons
    );
  }

  // Not used since we override Present, but required by abstract class
  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice>();
  }
}
