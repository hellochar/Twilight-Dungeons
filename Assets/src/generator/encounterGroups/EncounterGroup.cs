using static Encounters;

[System.Serializable]
public class EncounterGroup {
  public static EncounterGroup EarlyGame() => EncounterGroupEarlyGame.EarlyGame();
  public static EncounterGroup EarlyMidMixed() => EncounterGroupEverything.Everything();
  public static EncounterGroup MidGame() => EncounterGroupMidGame.MidGame();

  /// <summary>The core enemy spawns. Makes up the majority of the challenge.</summary>
  public WeightedRandomBag<Encounter> Mobs;
  /// <summary>On top of mobs, grasses, etc, we add some special variety random encounters
  /// to increase the interesting encounters and keep players guessing.</summary>
  public WeightedRandomBag<Encounter> Spice;
  /// <summary>The core grass spawns.</summary>
  public WeightedRandomBag<Encounter> Grasses;

  ///<summary>Walls, blockades, tile modifications.</summary>
  public WeightedRandomBag<Encounter> Walls;

  ///<summary>Reward encounters - these should be clear help to player.</summary>
  public WeightedRandomBag<Encounter> Rewards;
  ///<summary>Plant rewards - the big guns.</summmary>
  public WeightedRandomBag<Encounter> Plants;

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
    Walls = new WeightedRandomBag<Encounter> {
      { 3f, Empty },
      { 0.5f, WallPillars },
      { 0.5f, ChunkInMiddle },
      { 0.5f, LineWithOpening },
      { 0.5f, InsetLayerWithOpening },
    };
    Rewards = new WeightedRandomBag<Encounter> {
      { 1f, AddMushroom },
      { 1f, AddPumpkin },
      { 1f, OneAstoria },
      { 1f, AddThickBranch },
      { 1f, AddJackalHide },
      { 1f, AddGloopShoes },
      { 1f, OneButterfly },
    };
    Plants = new WeightedRandomBag<Encounter> {
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