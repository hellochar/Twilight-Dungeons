using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class UpstairsController : TileController {
  public Upstairs upstairs => (Upstairs) tile;

  public override void HandleInteracted(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    player.SetTasks(
      new MoveToTargetTask(player, tile.pos),
      new GenericPlayerTask(player, upstairs.TryGoHome)
    );
  }
}
