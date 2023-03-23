using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("fountain", description: "Purify your slime here.")]
public class Desalinator : Station, IInteractableInventory, IDaySteppable
{
  public override int maxDurability => 6;
  public ItemSlime slime => inventory[0] as ItemSlime;
  public override bool isActive => slime != null;

  public Desalinator(Vector2Int pos) : base(pos) {
    inventory.allowDragAndDrop = true;
   }

  // [PlayerAction]
  // public void Purify() {
  //   // if (this.slime != null) {
  //   //   throw new CannotPerformActionException("Slime already added!");
  //   // }
  //   var player = GameModel.main.player;
  //   // player.UseActionPointOrThrow();
  //   var slime = player.inventory.FirstOrDefault(i => i is ItemSlime) as ItemSlime;
  //   if (slime != null) {
  //     // this.slime = slime;
  //     player.inventory.RemoveItem(slime);
  //     slime.PurifyFree(player);
  //     this.ReduceDurability();
  //   }
  // }

  public void StepDay() {
    if (slime != null) {
      slime.PurifyFree(GameModel.main.player);
      this.ReduceDurability();
    }
  }
}
