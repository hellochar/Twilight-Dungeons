using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LeecherController : ActorController {
  Leecher leecher => (Leecher) actor;
  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    if (actor.IsDead) {
      return null; // don't do anything to dead actors
    }
    Player player = GameModel.main.player;
    return new SetTasksPlayerInteraction(
      new MoveNextToTargetTask(player, actor.pos),
      new GenericPlayerTask(player, leecher.Pickup)
    );
  }
}
