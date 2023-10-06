using System;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
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
  public EncounterBag Chasms;

  ///<summary>Reward encounters - these should be clear help to player.</summary>
  public EncounterBag Rewards;
  ///<summary>Plant rewards - the big guns.</summmary>
  public EncounterBag Plants;
  public EncounterBag Rests;

  public EncounterGroup AssignShared(EncounterGroupShared source) {
    Walls = source.Walls;
    Chasms = source.Chasms;
    Rewards = source.Rewards;
    Plants = source.Plants;
    Rests = source.Rests;
    return this;
  }

  public override string ToString() => $@"
Mobs: {Mobs}
Spice: {Spice}
Grasses: {Grasses}
Walls: {Walls}
Chasms: {Chasms}
Rewards: {Rewards}
Plants: {Plants}
Rests: {Rests}".TrimStart();
}

[System.Serializable]
public class EncounterGroupShared : EncounterGroup {
  public EncounterGroupShared() {
    Walls = new EncounterBag {
      { 8.5f, Empty },
      { 1, WallPillars },
      { 1, Concavity },
      { 1, ChunkInMiddle },
      { 1, LineWithOpening },
      { 1, InsetLayerWithOpening },
      { 1, AddStalk },
      { 1, RubbleCluster },
      // { 1, StalkCluster },
      // { 1, StumpCluster },
      { 0.5f, ChasmsAwayFromWalls2 }
    };
    Chasms = new EncounterBag {
      { 19, Empty },
      { 2, ChasmBridge },
      { 1, ChasmGrowths },
    };
    // spice for reward rooms
    Rewards = new EncounterBag {
      { 20, Empty },
      { 1, AddMushroom },
      { 1, AddPumpkin },
      { 1, AddGambler },
      // { 1, AddThickBranch },
      // { 1, AddBatTooth },
      // { 1, AddSnailShell },
      { 1, AddSpiderSandals },
      // { 1, AddJackalHide },
      // { 1, AddGloopShoes },
      { 1, OneButterfly },
    };
    Plants = new EncounterBag {
      { 1, MatureBerryBush },
      { 1, MatureThornleaf },
      { 1, MatureWildWood },
      { 1, MatureWeirdwood },
      { 1, MatureKingshroom },
      { 1, MatureFrizzlefen },
      { 1, MatureChangErsWillow },
      { 1, MatureStoutShrub },
      { 1, MatureBroodpuff },
      // { 1, MatureFaeleaf }
    };
    Rests = new EncounterBag {
      // { 1, Campfire },
      // { 1, }
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
      if (methodInfo != null) {
        var encounter = (Encounter) Delegate.CreateDelegate(typeof(Encounter), methodInfo);
        Add(weight, encounter);
      } else {
        Debug.LogWarning($"Couldn't find Encounter {methodName}.");
      }
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