using System;
using UnityEngine;

[Serializable]
[ObjectInfo("redleaf", "Produces healing when planted at home.")]
public class Redleaf : Grass, IDaySteppable, IActorEnterHandler {
  public Redleaf(Vector2Int pos) : base(pos) {
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player p) {
      BecomeItemInInventory(new ItemGrass(GetType(), 1), p);
    }
  }

  public void StepDay() {
    var player = GameModel.main.player;
    if (player.hp < player.maxHp) {
      player.Heal(1);
      KillSelf();
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
