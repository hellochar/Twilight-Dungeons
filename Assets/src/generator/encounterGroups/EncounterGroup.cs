using static Encounters;

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
  public WeightedRandomBag<Encounter> Grasses = new WeightedRandomBag<Encounter> {
    { 1f, AddSoftGrass },

    { 0.75f, AddBladegrass },

    { 0.5f, AddAgave },
    { 0.5f, AddHangingVines },

    { 0.4f, AddEveningBells },
    { 0.4f, AddGuardleaf },
    { 0.4f, AddSpore },
    { 0.4f, AddWebs },
    { 0.4f, ScatteredBoombugs },

    { 0.2f, AddPoisonmoss },
    { 0.2f, AddDeathbloom },
  };

  ///<summary>Walls, blockades, tile modifications.</summary>
  public WeightedRandomBag<Encounter> Walls = WallsStatic;

  ///<summary>Reward encounters - these should be clear help to player.</summary>
  public WeightedRandomBag<Encounter> Rewards = RewardsStatic;
  ///<summary>Plant rewards - the big guns.</summmary>
  public WeightedRandomBag<Encounter> Plants = PlantsStatic;

  public static WeightedRandomBag<Encounter> WallsStatic = new WeightedRandomBag<Encounter> {
    { 3f, Empty },
    { 0.5f, WallPillars },
    { 0.5f, ChunkInMiddle },
    { 0.5f, LineWithOpening },
  };
  public static WeightedRandomBag<Encounter> RewardsStatic = new WeightedRandomBag<Encounter> {
    { 1f, AddMushroom },
    { 1f, AddPumpkin },
    { 1f, OneAstoria },
    { 1f, AddJackalHide },
    { 1f, AddGloopShoes },
    { 1f, OneButterfly },
  };
  public static WeightedRandomBag<Encounter> PlantsStatic = new WeightedRandomBag<Encounter> {
    { 1f, MatureBerryBush },
    { 1f, MatureThornleaf },
    { 1f, MatureWildWood },
    { 1f, MatureWeirdwood },
    { 1f, MatureKingshroom }
  };
}