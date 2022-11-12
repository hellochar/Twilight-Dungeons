using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class TeleporterController : TileController, IOnTopActionHandler {
  public Teleporter t => (Teleporter) tile;

  public string OnTopActionName => "Go Home";

  public void HandleOnTopAction() {
    // "downstairs"
    t.TryGoHome();
  }
}
