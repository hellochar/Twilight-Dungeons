using System;
using UnityEngine;

[Serializable]
[ObjectInfo("Astoria", "Plant at home to heal you 1 HP per floor.")]
public class Redpod : Grass, IDaySteppable, IActorEnterHandler {
  public Redpod(Vector2Int pos) : base(pos) {
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player p) {
      // BecomeItemInInventory(new ItemGrass(GetType(), 1), p);
      BecomeItemInInventory(new ItemPlaceableEntity(new Redpod(new Vector2Int())), p);
    }
  }

  public void StepDay() {
    var player = GameModel.main.player;
    if (player.hp < player.maxHp) {
      player.Heal(1);
      // KillSelf();
    }
    // floor.Put(new ItemOnGround(pos, new ItemRedleaf(), pos));
  }
}

[System.Serializable]
[ObjectInfo("redleaf")]
class ItemRedleaf : Item, IEdible {
  public void Eat(Actor a) {
    a.Heal(1);
    Destroy();
  }

  internal override string GetStats() => "Heals 1 HP.";
}
