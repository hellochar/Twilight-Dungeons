using System;
using static Encounters;

[System.Serializable]
public class EncounterGroup {
  public static EncounterGroup EarlyGame() => EncounterGroupEarlyGame.EarlyGame();
  public static EncounterGroup EarlyMidMixed() => EncounterGroupEverything.Everything();
  public static EncounterGroup MidGame() => EncounterGroupMidGame.MidGame();
  public static EncounterGroup ActuallyEverything() => EarlyGame()
    .Merge(EarlyMidMixed())
    .Merge(MidGame());

  /// <summary>The core enemy spawns. Makes up the majority of the challenge.</summary>
  public EncounterBag Mobs;
  /// <summary>On top of mobs, grasses, etc, we add some special variety random encounters
  /// to increase the interesting encounters and keep players guessing.</summary>
  public EncounterBag Spice;
  /// <summary>The core grass spawns.</summary>
  public EncounterBag Grasses;
  public EncounterBag DayChange = new EncounterBag();

  ///<summary>Walls, blockades, tile modifications.</summary>
  public EncounterBag Walls;
  public EncounterBag Chasms;

  ///<summary>Reward encounters - these should be clear help to player.</summary>
  public EncounterBag Rewards;
  ///<summary>Plant rewards - the big guns.</summmary>
  public EncounterBag Plants;
  public EncounterBag Rests;

  public EncounterGroup Merge(EncounterGroup other) {
    Mobs.Merge(other.Mobs);
    Spice.Merge(other.Spice);
    Grasses.Merge(other.Grasses);
    return this;
  }

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
DayChange: {DayChange}
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
      { 7.5f, Empty },
      { 1, WallPillars },
      { 1, ChunkInMiddle },
      { 1, LineWithOpening },
      { 1, InsetLayerWithOpening },
      { 1, AddStalk },
      { 0.5f, ChasmsAwayFromWalls2 }
    };
    Chasms = new EncounterBag {
      { 19, Empty },
      { 1, ChasmBridge },
    };
    Rewards = new EncounterBag {
      { 1, AddPumpkin },
      { 1, AddThickBranch },
      { 1, AddBatTooth },
      { 1, AddSnailShell },
      { 1, AddSpiderSandals },
      { 1, AddJackalHide },
      { 1, AddGloopShoes },
      { 1, OneButterfly },
      { 1f, AddNubs },
      { 1f, AddRedleaf },
      // { 1, AddSoil },
      // { 1, AddCrafting },
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
    };
    Rests = new EncounterBag {
      // { 1, Campfire },
      // { 1, }
    };
  }
}

[Serializable]
public class EncounterBag : WeightedRandomBag<Encounter> {
  public EncounterBag() : base() { }
}