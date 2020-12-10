using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoilController : TileController {
  public override void OnPointerClick(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    ItemSeed seed = (ItemSeed)player.inventory.ToList().Find(item => item is ItemSeed);
    if (seed != null) {
      player.SetTasks(
        new MoveNextToTargetTask(player, owner.pos),
        new GenericTask(player, (p) => {
          if (p.IsNextTo(owner)) {
            seed.Plant((Soil)owner);
          }
        })
      );
    } else {
      base.OnPointerClick(pointerEventData);
    }
  }
}
