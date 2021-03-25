using static Encounters;

public static class EncounterGroupMidGame {
  public static EncounterGroup MidGame() {
    return new EncounterGroup() {
      Mobs = new EncounterBag {
        { 1f, AddScorpions },
        { 1f, AddWildekins },
        { 1f, AddCrabs },
        { 1f, AddThistlebog },
        { 0.5f, AddGrasper },
        { 0.5f, AddGolems },
        { 0.5f, AddHydra },
        { 0.4f, AddParasite },
      },

      Grasses = new EncounterBag {
        { 0.75f, AddViolets },
        { 0.75f, AddTunnelroot },

        { 0.5f, AddHangingVines2x },
        { 0.5f, AddDeathbloom },
        { 0.5f, AddBrambles },
        { 0.5f, AddPoisonmoss },
        { 0.5f, AddSpore },

        { 0.4f, ScatteredBoombugs },

        { 0.3f, AddEveningBells },

        { 0.2f, AddWebs2x },
        { 0.2f, AddGuardleaf2x },

        { 0.1f, AddSoftGrass4x },
        { 0.1f, AddBladegrass4x },

        // { 0.0f, AddAgave },
      },

      Spice = new EncounterBag {
        { 3f, Empty },

        { 0.25f, AddEveningBells },
        { 0.25f, AddPoisonmoss },
        { 0.25f, AddViolets },
        { 0.25f, AddBrambles },
        { 0.25f, AddTunnelroot4x },
        { 0.25f, ScatteredBoombugs4x },
        { 0.25f, AddFruitingBodies },

        { 0.2f, AddWater },

        { 0.2f, AddSpiders },
        { 0.2f, AddGolems },
        { 0.2f, AddScorpions },
        { 0.2f, AddWildekins },
        { 0.2f, AddCrabs },
        { 0.05f, AddParasite8x },

        { 0.1f, AddHydra },
        { 0.1f, AddGrasper },
        { 0.1f, AddDeathbloom },
        { 0.1f, AddSpore },

        { 0.02f, AddNecroroot },

        // { 0.1f, Twice(Twice(Twice(JackalPile))) },
        // { 0.1f, Twice(Twice(Twice(AFewSnails))) },
      },
    };
  }
}