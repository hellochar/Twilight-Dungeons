using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("fountain", description: "Purify your slime here.")]
public class Desalinator : Station
// , IDaySteppable
{
  public override int maxDurability => 6;
  // ItemSlime slime;
  // public override bool isActive => slime != null;
  public override bool isActive => false;

  public Desalinator(Vector2Int pos) : base(pos) { }

  [PlayerAction]
  public void Purify() {
    // if (this.slime != null) {
    //   throw new CannotPerformActionException("Slime already added!");
    // }
    var player = GameModel.main.player;
    // player.UseActionPointOrThrow();
    var slime = player.inventory.FirstOrDefault(i => i is ItemSlime) as ItemSlime;
    if (slime != null) {
      // this.slime = slime;
      player.inventory.RemoveItem(slime);
      slime.PurifyFree(player);
      this.ReduceDurability();
    }
  }

  // public void StepDay() {
  //   if (slime != null) {
  //     slime.PurifyFree(GameModel.main.player);
  //     slime = null;
  //     this.ReduceDurability();
  //   }
  // }
}
