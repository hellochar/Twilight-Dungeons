using static Encounters;

public static class EncounterGroupMidGame {
  public static EncounterGroup MidGame() {
    return new EncounterGroup() {
      Mobs = new WeightedRandomBag<Encounter> {
        { 1f, AddScorpions },
        { 1f, AddWildekins },
        { 1f, AddCrabs },
        { 1f, AddThistlebog },
        { 0.5f, AddGrasper },
        { 0.5f, AddGolems },
        { 0.5f, AddHydra },
        { 0.4f, AddParasite },
      },

      Grasses = new WeightedRandomBag<Encounter> {
        { 1f, AddViolets },

        { 0.75f, AddTunnelroot },

        { 0.5f, Twice(AddHangingVines) },
        { 0.5f, AddDeathbloom },
        { 0.5f, AddBrambles },
        { 0.5f, AddPoisonmoss },

        { 0.4f, AddSpore },
        { 0.4f, ScatteredBoombugs },

        { 0.3f, AddEveningBells },

        { 0.2f, Twice(AddWebs) },
        { 0.2f, Twice(AddGuardleaf) },

        { 0.1f, Twice(Twice(AddSoftGrass)) },
        { 0.1f, Twice(Twice(AddBladegrass)) },

        // { 0.0f, AddAgave },
      },

      Spice = new WeightedRandomBag<Encounter> {
        { 3f, Empty },

        { 0.25f, AddEveningBells },
        { 0.25f, AddPoisonmoss },
        { 0.25f, AddTunnelroot },
        { 0.25f, AddViolets },
        { 0.25f, AddBrambles },
        { 0.25f, Twice(AddTunnelroot) },
        { 0.25f, Twice(Twice(ScatteredBoombugs)) },

        { 0.2f, AddWater },

        { 0.2f, AddSpiders },
        { 0.2f, AddGolems },
        { 0.2f, AddScorpions },
        { 0.2f, AddWildekins },
        { 0.2f, AddCrabs },
        //// adding extra parasites is just fucking annoying tbh
        // { 0.05f, AddParasite },

        { 0.1f, AddHydra },
        { 0.1f, AddGrasper },
        { 0.1f, AddDeathbloom },
        { 0.1f, AddSpore },

        { 0.1f, Twice(Twice(Twice(JackalPile))) },
        { 0.1f, Twice(Twice(Twice(AFewSnails))) },
      },
    };
  }
}