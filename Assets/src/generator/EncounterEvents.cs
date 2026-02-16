using UnityEngine;
using System.Linq;

/// <summary>
/// Encounter methods that place NarrativeEvent EventBody entities on floors.
/// These are wired into EncounterBag Spice bags so they appear as random encounters.
/// </summary>
public partial class Encounters {
  public static void AddTransmutationAltar(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room).Where(t => t.grass == null));
    if (tile != null) {
      floor.Put(new EventBody(tile.pos, new TransmutationAltarEvent()));
    }
  }

  public static void AddHealingSpring(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room).Where(t => t.grass == null));
    if (tile != null) {
      floor.Put(new EventBody(tile.pos, new HealingSpringEvent()));
    }
  }

  public static void AddFertileAsh(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room).Where(t => t.grass == null));
    if (tile != null) {
      floor.Put(new EventBody(tile.pos, new FertileAshEvent()));
    }
  }
}
