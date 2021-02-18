using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TeleportStoneController : ActorController {
  // public TeleportStone stone => (TeleportStone)actor;
  public new ParticleSystem particleSystem;

  void OnEnable() {
    var model = GameModel.main;
    if (model.depth == 0) {
      particleSystem.gameObject.SetActive(false);
    } else {
      particleSystem.gameObject.SetActive(true);
    }
  }

  // public override void PointerClick(PointerEventData pointerEventData) {
  //   var player = GameModel.main.player;
  //   player.SetTasks(
  //     new MoveNextToTargetTask(player, stone.pos),
  //     new GenericTask(player, (_) => stone.TeleportPlayer())
  //   );
  // }
}
