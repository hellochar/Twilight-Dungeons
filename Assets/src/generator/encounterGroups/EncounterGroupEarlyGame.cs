using static Encounters;

public static class EncounterGroupEarlyGame {
  public static EncounterGroup EarlyGame() {
    return new EncounterGroup() {
      Mobs = new WeightedRandomBag<Encounter> {
        { 1, AFewBlobs },
        { 1, JackalPile },
        { 1, AFewSnails },
        { 0.4f, AddBats },
        { 0.35f, AddSpiders },
      },

      Spice = new WeightedRandomBag<Encounter> {
        { 5f, Empty },

        { 0.5f, AFewBlobs },
        { 0.5f, JackalPile },
        { 0.5f, AFewSnails },

        { 0.25f, AddSoftGrass },
        { 0.25f, AddBladegrass },

        { 0.2f, AddWater },
        { 0.2f, ScatteredBoombugs },

        // { 0.1f, AddCoralmoss },
        { 0.1f, AddDeathbloom },
        { 0.1f, AddSpiders },
        { 0.1f, AddGuardleaf },
        { 0.1f, AddSpore },

        { 0.05f, AddEveningBells },
        { 0.05f, AddPoisonmoss },
      },
    };
  }
}