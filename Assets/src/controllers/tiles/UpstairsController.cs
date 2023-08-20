using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class UpstairsController : TileController, IOnTopActionHandler {
  public Upstairs upstairs => (Upstairs) tile;

  public string OnTopActionName => ((IOnTopActionHandler)upstairs).OnTopActionName;
  public void HandleOnTopAction() {
      ((IOnTopActionHandler)upstairs).HandleOnTopAction();
  }
}
