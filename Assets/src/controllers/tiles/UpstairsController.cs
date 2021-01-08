using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class UpstairsController : TileController {
  public Upstairs upstairs => (Upstairs) tile;

  public override void PointerClick(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    player.SetTasks(
      new MoveNextToTargetTask(player, tile.pos),
      new GenericTask(player, (p) => {
        upstairs.GoHome();
      })
    );
  }
}
