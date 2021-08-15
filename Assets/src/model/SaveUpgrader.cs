public static class SaveUpgrader {
  public static void Upgrade(GameModel model) {
    FixPosZeroBug(model);
  }

  // BUGFIX - prior to 1.10.0 the _pos was erroneously marked nonserialized, meaning
  // players would lose all items on the ground when they saved and loaded.
  // Thankfully, the "start" position kind of keeps track of it, so we graft it onto
  // the pos and then 
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

}