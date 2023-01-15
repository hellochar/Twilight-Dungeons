using System;
using UnityEngine;

[Serializable]
[ObjectInfo("nubs", description: "Plant at home to produce 8 water per floor.")]
public class Nubs : Grass, IActorEnterHandler {
  public Nubs(Vector2Int pos) : base(pos) {
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player p) {
      BecomeItemInInventory(new ItemGrass(GetType()), p);
      // BecomeItemInInventory(new ItemPlaceableEntity(new Nubs(new Vector2Int())), p);
    }
  }

  // public void HandleActorLeave(Actor who) {
  //   if (floor.depth != 0) {
  //     Kill(who);
  //   }
  // }

  public override void StepDay() {
    base.StepDay();
    GameModel.main.player.water += 8;
  }
}
