using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Heals you for 4 HP if you're hurt.\nIf you're full HP, you can pick up the Astoria and use it later.")]
public class Astoria : Grass, IActorEnterHandler {
  public Astoria(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor actor) {
    if (actor is Player p) {
      if (actor.hp < actor.maxHp) {
        actor.Heal(4);
        Kill(actor);
      } else {
        BecomeItemInInventory(new ItemAstoria(), p);
      }
    }
  }
}

[ObjectInfo("Astoria")]
class ItemAstoria : Item, IUsable {
  public void Use(Actor a) {
    a.Heal(4);
    Destroy();
  }

  internal override string GetStats() => "Heals 4 HP.";
}