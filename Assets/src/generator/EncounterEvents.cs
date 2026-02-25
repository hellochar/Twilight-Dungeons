using UnityEngine;
using System.Linq;

/// <summary>
/// Encounter methods that place NarrativeEvent EventBody entities on floors.
/// These are wired into EncounterBag Spice bags so they appear as random encounters.
/// </summary>
public partial class Encounters {
  private static void PlaceEventBody(Floor floor, Room room, NarrativeEvent evt) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room).Where(t => t.grass == null));
    if (tile != null) {
      floor.Put(new EventBody(tile.pos, evt));
    }
  }

  // Category 1: Transmutation
  public static void AddTransmutationAltar(Floor floor, Room room) => PlaceEventBody(floor, room, new TransmutationAltarEvent());

  // Category 3: Body Mutation
  public static void AddFungalPool(Floor floor, Room room) => PlaceEventBody(floor, room, new FungalPoolEvent());

  // Category 5: Parasite/Symbiote
  public static void AddClingingVine(Floor floor, Room room) => PlaceEventBody(floor, room, new ClingingVineEvent());

  // Category 7: Environmental Shift
  public static void AddCrumblingSeal(Floor floor, Room room) => PlaceEventBody(floor, room, new CrumblingSealEvent());

  // Category 8: Sealed Encounter
  public static void AddSealedChamber(Floor floor, Room room) => PlaceEventBody(floor, room, new SealedChamberEvent());

  // Category 9: The Vendor
  public static void AddRootMerchant(Floor floor, Room room) => PlaceEventBody(floor, room, new RootMerchantEvent());

  // Category 10: Resonance
  public static void AddWhisperingMoss(Floor floor, Room room) => PlaceEventBody(floor, room, new WhisperingMossEvent());

  // Category 11: Companion
  public static void AddDormantGolem(Floor floor, Room room) => PlaceEventBody(floor, room, new DormantGolemEvent());

  // Category 13: Curse/Gift Duality
  public static void AddThornbloodPact(Floor floor, Room room) => PlaceEventBody(floor, room, new ThornbloodPactEvent());

  // Category 14: Resource Wellspring
  public static void AddHealingSpring(Floor floor, Room room) => PlaceEventBody(floor, room, new HealingSpringEvent());

  // Category 15: Graveyard/Memory
  public static void AddFallenExplorer(Floor floor, Room room) => PlaceEventBody(floor, room, new FallenExplorerEvent());

  // Category 16: The Wager
  public static void AddStoneDice(Floor floor, Room room) => PlaceEventBody(floor, room, new StoneDiceEvent());

  // Category 17: Garden Ritual
  public static void AddFertileAsh(Floor floor, Room room) => PlaceEventBody(floor, room, new FertileAshEvent());
}
