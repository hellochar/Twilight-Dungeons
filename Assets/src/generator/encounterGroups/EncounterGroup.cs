using System;
using System.Linq;
using System.Runtime.Serialization;
using static Encounters;

[System.Serializable]
public class EncounterGroup {
  public static EncounterGroup EarlyGame() => EncounterGroupEarlyGame.EarlyGame();
  public static EncounterGroup EarlyMidMixed() => EncounterGroupEverything.Everything();
  public static EncounterGroup MidGame() => EncounterGroupMidGame.MidGame();

  /// <summary>The core enemy spawns. Makes up the majority of the challenge.</summary>
  public EncounterBag Mobs;
  /// <summary>On top of mobs, grasses, etc, we add some special variety random encounters
  /// to increase the interesting encounters and keep players guessing.</summary>
  public EncounterBag Spice;
  /// <summary>The core grass spawns.</summary>
  public EncounterBag Grasses;

  ///<summary>Walls, blockades, tile modifications.</summary>
  public EncounterBag Walls;

  ///<summary>Reward encounters - these should be clear help to player.</summary>
  public EncounterBag Rewards;
  ///<summary>Plant rewards - the big guns.</summmary>
  public EncounterBag Plants;

  public EncounterGroup AssignShared(EncounterGroupShared source) {
    Walls = source.Walls;
    Rewards = source.Rewards;
    Plants = source.Plants;
    return this;
  }
}

[System.Serializable]
public class EncounterGroupShared : EncounterGroup {
  public EncounterGroupShared() {
    Walls = new EncounterBag {
      { 3f, Empty },
      { 0.5f, WallPillars },
      { 0.5f, ChunkInMiddle },
      { 0.5f, LineWithOpening },
      { 0.5f, InsetLayerWithOpening },
    };
    Rewards = new EncounterBag {
      { 1f, AddMushroom },
      { 1f, AddPumpkin },
      { 1f, AddThickBranch },
      { 1f, AddJackalHide },
      { 1f, AddGloopShoes },
      { 1f, OneButterfly },
    };
    Plants = new EncounterBag {
      { 1f, MatureBerryBush },
      { 1f, MatureThornleaf },
      { 1f, MatureWildWood },
      { 1f, MatureWeirdwood },
      { 1f, MatureKingshroom },
      { 1f, MatureFrizzlefen },
      { 1f, MatureChangErsWillow }
    };
  }
}

[Serializable]
public class EncounterBag : WeightedRandomBag<Encounter>, ISerializable {
  public EncounterBag() : base() { }

  public EncounterBag(SerializationInfo info, StreamingContext context) : base() {
    var length = info.GetInt32("length");
    for(int i = 0; i < length; i++) {
      var weight = info.GetSingle($"item-{i}-weight");
      var methodName = info.GetString($"item-{i}-name");
      var methodInfo = typeof(Encounters).GetMethod(methodName);
      var encounter = (Encounter) Delegate.CreateDelegate(typeof(Encounter), methodInfo);
      Add(weight, encounter);
    }
  }

  public void GetObjectData(SerializationInfo info, StreamingContext context) {
    info.AddValue("length", entries.Count);
    for(int i = 0; i < entries.Count; i++) {
      var entry = entries[i];
      info.AddValue($"item-{i}-weight", entry.weight);
      info.AddValue($"item-{i}-name", entry.item.Method.Name);
    }
  }
}