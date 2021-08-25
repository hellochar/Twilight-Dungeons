using System;
using System.Linq;
using UnityEngine;

public static class SaveUpgrader {
  public static void Upgrade(GameModel model) {
    EnsureVersionStringExists(model);
    // if you're below 1.10.0, run FixPosZeroBug on the model
    MaybeRunUpgrader("1.10.0", FixPosZeroBug, model);
    MaybeRunUpgrader("1.11.0", AddFieldVibrantIvyStacks, model);
  }

  private static void MaybeRunUpgrader(string firstGoodVersionString, Action<GameModel> Upgrade, GameModel model) {
    var modelVersion = new Version(model.version);
    var firstGoodVersion = new Version(firstGoodVersionString);
    if (modelVersion < firstGoodVersion) {
      Upgrade(model);
      Debug.Log($"Upgraded {model.version} to {firstGoodVersionString}");
      model.version = firstGoodVersionString;
    }
  }

  private static void EnsureVersionStringExists(GameModel model) {
    // We've been compatible since version 1.8.0 so take the worst case scenario.
    if (model.version == null) {
      model.version = "1.8.0";
    }
  }

  // BUGFIX - prior to 1.10.0 the _pos was erroneously marked nonserialized, meaning
  // players would lose all items on the ground when they saved and loaded.
  // Thankfully, the position was cached in the floor's StaticEntityGrid, so we can
  // restore the pos from that
  private static void FixPosZeroBug(GameModel model) {
    void Fix(StaticEntityGrid<ItemOnGround> grid) {
      for (int x = 0; x < grid.width; x++) {
        for (int y = 0; y < grid.height; y++) {
          var item = grid[x, y];
          // Find item whose pos doesn't match the grid; use Grid as source of truth
          if (item != null && (item.pos.x != x || item.pos.y != y)) {
            item.FixPosZeroBug(x, y);
          }
        }
      }
    }
    Fix(model.home.items);
    Fix(model.cave.items);
  }

  private static void AddFieldVibrantIvyStacks(GameModel model) {
    void Fix(Floor floor) {
      foreach (var ivy in floor.grasses.Where(g => g is VibrantIvy).Cast<VibrantIvy>()) {
        ivy.ComputeStacks();
      }
    }
    Fix(model.home);
    Fix(model.cave);
  }
}