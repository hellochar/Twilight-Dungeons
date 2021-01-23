using static Encounters;

public static class EncounterGroupMidGame {
  public static EncounterGroup MidGame() {
    return new EncounterGroup() {
      Mobs = new WeightedRandomBag<Encounter> {
        { 1f, AddScorpions },
        { 1f, AddGolems },
        { 1f, AddWildekins },
        { 1f, AddCrabs },
        // need a 3rd 1f midgame mob
        { 0.5f, AddHydra },
        { 0.4f, AddParasite },
      },
      Spice = new WeightedRandomBag<Encounter> {
        { 3f, Empty },

        { 0.25f, AddEveningBells },
        { 0.25f, AddPoisonmoss },
        { 0.25f, Twice(ScatteredBoombugs) },
        { 0.25f, Twice(AddGuardleaf) },

        { 0.2f, AddWater },

        { 0.2f, AddSpiders },
        { 0.2f, AddGolems },
        { 0.2f, AddScorpions },
        { 0.2f, AddWildekins },
        { 0.2f, AddCrabs },
        //// adding extra parasites is just fucking annoying tbh
        // { 0.05f, AddParasite },

        { 0.1f, AddHydra },
        { 0.1f, AddDeathbloom },
        { 0.1f, AddSpore },

        { 0.05f, Twice(Twice(AddSoftGrass)) },
        { 0.05f, Twice(Twice(AddBladegrass)) },
      },
    };
  }
}