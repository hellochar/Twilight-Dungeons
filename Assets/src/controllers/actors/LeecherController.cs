using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LeecherController : ActorController {
  Leecher leecher => (Leecher) actor;
  public override void HandleInteracted(PointerEventData pointerEventData) {
    if (actor.IsDead) {
      return; // don't do anything to dead actors
    }
    Player player = GameModel.main.player;
    player.SetTasks(
      new MoveNextToTargetTask(player, actor.pos),
      new GenericPlayerTask(player, leecher.Pickup)
    );
  }
}
