using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HardGroundController : TileController , IOnTopActionHandler {

  public string OnTopActionName => "Soften";

  public void HandleOnTopAction() {
    ((HardGround)tile).Soften();
  }
}