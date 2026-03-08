using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 8: Sealed Encounter — optional dangerous fight with good loot.
/// Placed as an in-world EventBody.
/// </summary>
[Serializable]
public class SealedChamberEvent : NarrativeEvent {
  public override string Title => "Sealed Chamber";
  public override string Description => "Behind a cracked stone door, something massive shifts. The air hums with danger — and promise.";
  public override string FlavorText => "\"Only the bold claim what the depths protect.\"";
  public override int MinDepth => 6;
  public override int MaxDepth => 24;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Break the seal",
        Tooltip = "A powerful enemy appears — with valuable loot nearby",
        Effect = (c) => {
          var floor = GameModel.main.currentFloor;
          var playerPos = c.player.pos;

          // Find a tile to spawn the enemy
          var enemyTile = floor.BreadthFirstSearch(playerPos, t => true)
            .Where(t => t.CanBeOccupied() && t.pos != playerPos)
            .Skip(3)
            .FirstOrDefault();

          if (enemyTile != null) {
            floor.Put(new Golem(enemyTile.pos));

            // Place loot near the enemy
            var lootTile = floor.GetAdjacentTiles(enemyTile.pos)
              .Where(t => t.CanBeOccupied())
              .FirstOrDefault();
            if (lootTile != null) {
              var loot = GetLootForDepth(c.currentDepth);
              floor.Put(new ItemOnGround(lootTile.pos, loot));
            }
          }
        }
      },
      new EventChoice {
        Label = "Walk away",
        Tooltip = "Some doors are better left closed",
        Effect = (c) => { }
      }
    };
  }

  private Item GetLootForDepth(int depth) {
    if (depth < 12) {
      return new ItemThickBranch();
    } else {
      var types = new Type[] { typeof(Kingshroom), typeof(Weirdwood), typeof(Frizzlefen) };
      return new ItemSeed(types[MyRandom.Range(0, types.Length)], 1);
    }
  }
}
