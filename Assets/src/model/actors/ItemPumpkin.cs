public class ItemPumpkin : Item, IEdible {
  public ItemPumpkin() {}

  public void Eat(Actor a) {
    if (a is Player p) {
      p.IncreaseFullness(0.5f);
      var helmet = new ItemPumpkinHelmet();
      if (!p.inventory.AddItem(helmet, a)) {
        var itemOnGround = new ItemOnGround(a.pos, helmet);
      }
    }
    Destroy();
  }

  internal override string GetStats() => "Restores 50% food.\nMakes a nice helmet after you eat it.";
}
