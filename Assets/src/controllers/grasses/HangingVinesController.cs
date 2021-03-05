using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HangingVinesController : GrassController, IPlayerInteractHandler {
  public HangingVines vines => (HangingVines) grass;

  public void HandleInteracted(PointerEventData eventData) {
    Player player = GameModel.main.player;
    player.SetTasks(
      new MoveNextToTargetTask(player, vines.pos),
      new AttackGroundTask(player, vines.pos)
    );
  }
}
