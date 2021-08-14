using static Encounters;

public static class EncounterGroupMidGame {
  public static EncounterGroup MidGame() {
    return new EncounterGroup() {
      Mobs = new EncounterBag {
        { 1f, AddHoppers },
        { 1f, AddScorpions },
        { 1f, AddWildekins },
        { 0.6f, AddIronJelly },
        { 0.6f, AddGolems },
        { 0.6f, AddHydra },
        { 0.5f, AddThistlebog },
        { 0.5f, AddGrasper },
        { 0.25f, AddParasite },
      },

      Grasses = new EncounterBag {
        { 0.75f, AddCrabs },
        { 0.75f, AddViolets },
        { 0.75f, AddTunnelroot },
        { 0.75f, AddGoldGrass },
        { 0.75f, AddRedcaps },

        { 0.5f, AddVibrantIvy },
        { 0.5f, AddHangingVines2x },
        { 0.5f, AddDeathbloom },
        { 0.5f, AddPoisonmoss },
        { 0.5f, AddSpore },

        { 0.3f, AddEveningBells },

        { 0.2f, ScatteredBoombugs4x },
        { 0.2f, AddGuardleaf4x },
        { 0.2f, AddBrambles },

        { 0.05f, AddNecroroot },
        { 0.05f, FillWithFerns }
      },

      Spice = new EncounterBag {
        { 3f, Empty },

        { 0.25f, AddEveningBells },
        { 0.25f, AddPoisonmoss },
        { 0.25f, AddViolets },
        { 0.25f, AddBrambles },
        { 0.25f, AddTunnelroot4x },
        { 0.25f, AddFruitingBodies },

        { 0.2f, AddHoppers },
        { 0.2f, AddIronJelly },
        { 0.2f, AddSpiders },
        { 0.2f, AddGolems },
        // { 0.2f, AddScorpions },
        // { 0.2f, AddWildekins },
        // { 0.2f, AddCrabs },

        { 0.1f, AddWater },
        { 0.1f, AddHydra },
        { 0.1f, AddGrasper },
        { 0.1f, AddDeathbloom },
        { 0.1f, AddSpore },
        { 0.1f, FillWithFerns },

        { 0.05f, AddParasite8x },

        { 0.02f, AddNecroroot },
      },
    };
  }
}