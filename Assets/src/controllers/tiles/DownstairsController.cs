using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class DownstairsController : TileController, IOnTopActionHandler {
  public Downstairs downstairs => (Downstairs) tile;

  public string OnTopActionName => downstairs.OnTopActionName;
  public void HandleOnTopAction() {
    downstairs.HandleOnTopAction();
  }

  public override void Start() {
    base.Start();
    if (downstairs.floor.depth == 9 - 1) {
        PrefabCache.Effects.Instantiate("Stairs Before Blobmother", transform);
    } else if (downstairs.floor.depth == 18 - 1) {
      PrefabCache.Effects.Instantiate("Stairs Before Fungal Colony", transform);
    }
  }
}
