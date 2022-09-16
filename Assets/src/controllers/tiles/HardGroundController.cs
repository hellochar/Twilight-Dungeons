using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if !experimental_grasscovering
public class HardGroundController : TileController {}
#else
public class HardGroundController : TileController , IOnTopActionHandler {

  public string OnTopActionName => "Soften";

  public void HandleOnTopAction() {
    ((HardGround)tile).Soften();
  }
}
#endif