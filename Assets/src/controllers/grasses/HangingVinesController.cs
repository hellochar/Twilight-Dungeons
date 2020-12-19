using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HangingVinesController : GrassController, IEntityClickedHandler {
  public HangingVines vines => (HangingVines) grass;

  public void PointerClick(PointerEventData eventData) {
    Player player = GameModel.main.player;
    player.SetTasks(
      new MoveNextToTargetTask(player, vines.pos),
      new AttackGroundTask(player, vines.pos)
    );
  }
}
