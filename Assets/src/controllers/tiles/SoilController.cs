using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoilController : TileController {
  public override void PointerClick(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    ItemSeed seed = (ItemSeed) player.inventory.ToList().Find(item => item is ItemSeed);
    if (seed != null) {
      player.SetTasks(
        new MoveNextToTargetTask(player, tile.pos),
        new GenericPlayerTask(player, () => {
          if (player.IsNextTo(tile)) {
            seed.Plant((Soil) tile);
          }
        })
      );
    } else {
      base.PointerClick(pointerEventData);
    }
  }
}
