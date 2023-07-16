public static class EntityExtensions {
  public static void BecomeItemInInventory(this Entity e, Item item) {
    var player = GameModel.main.player;
    var floor = e.floor;
    e.Kill(player);
    if (!player.inventory.AddItem(item, e)) {
      floor.Put(new ItemOnGround(e.pos, item, e.pos));
    }
  }
}
