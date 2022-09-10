using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Harmless.")]
public class CheshireWeed : Body {
  public CheshireWeed(Vector2Int pos) : base(pos) {
  }
}

[System.Serializable]
[ObjectInfo(description: "Any Creature walking over it takes 1 attack damage and clears the Sprout.\nAfter five turns, grows into a Cheshire Weed.")]
public class CheshireWeedSprout : Grass, ISteppable, IActorEnterHandler {
  public static bool CanOccupy(Tile tile) => tile.grass == null && tile.CanBeOccupied() && tile is Ground;
  public bool isMature => age >= 4;
  public CheshireWeedSprout(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
  }

  public float Step() {
    if (isMature) {
      floor.Put(new CheshireWeed(pos));
      KillSelf();
      return 1;
    }
    if (age <= 2) {
      var tiles = floor.GetCardinalNeighbors(pos).Where(CanOccupy);
      floor.PutAll(tiles.Select(tile => new CheshireWeedSprout(tile.pos)));
    }
    return 1;
  }

  public float timeNextAction { get; set; }

  public float turnPriority => 9;

  public void HandleActorEnter(Actor who) {
    if (isMature) {
      who.statuses.Add(new WeaknessStatus(1));
    }
    Kill(who);
  }
}