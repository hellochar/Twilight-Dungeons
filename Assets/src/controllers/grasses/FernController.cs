using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FernController : GrassController, IOnTopActionHandler {
  public Fern fern => (Fern) grass;

  public string OnTopActionName => "Cut";

  public void HandleOnTopAction() {
    Player player = GameModel.main.player;
    player.task = new GenericPlayerTask(player, () => fern.CutDown(player));
  }
}
