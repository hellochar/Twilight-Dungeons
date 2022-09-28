using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DesalinatorController : BodyController {
  public Desalinator station => (Desalinator) body;

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (body.IsDead) {
      return base.GetPlayerInteraction(pointerEventData); // don't do anything to dead actors
    }
    
    return new SetTasksPlayerInteraction(
      new MoveNextToTargetTask(player, body.pos),
      new GenericPlayerTask(player, ShowDialog)
    );
  }

  void ShowDialog() {
    InteractionController.ShowPopupFor(station);
  }
}
