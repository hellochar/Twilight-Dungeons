using static Encounters;

public static class EncounterGroupMidGame {
  public static EncounterGroup MidGame() {
    return new EncounterGroup() {
      Mobs = new EncounterBag {
        { 1f, AddHoppers },
        { 1f, AddScorpions },
        { 1f, AddWildekins },
        { 1f, AddDizapper },
        { 1f, AddGoo },
        { 1f, AddHardShell },
        { 0.5f, AddHealer },
        { 0.5f, AddPoisoner },
        // { 0.5f, AddVulnera },
        { 0.5f, AddMuckola },
        // { 0.5f, AddPistrala },
        { 0.5f, AddGolems },
        { 0.5f, AddHydra },
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
        { 5f, Empty },

        { 0.5f, AddIronJelly },
        { 0.25f, AddTunnelroot4x },
        { 0.25f, AddBloodstone },
        { 0.25f, AddScuttlers4x },
        { 0.25f, AddMercenary },
        { 0.25f, AddSpore8x },
        { 0.25f, FillWithBladegrass },
        { 0.25f, FillWithGuardleaf },
        { 0.25f, FillWithViolets },
        { 0.25f, FillWithFerns },
        // { 0.1f, AddPistrala },
        // { 0.2f, AddScorpions },
        // { 0.2f, AddWildekins },
        // { 0.2f, AddCrabs },


        { 0.05f, AddParasite8x },

        { 0.02f, AddFaegrass },
        { 0.02f, AddNecroroot },
      },
    };
  }
}