using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class DownstairsController : TileController {
  public Downstairs downstairs => (Downstairs) tile;

  public override void Start() {
    base.Start();
    if (downstairs.floor.depth == 11) {
      PrefabCache.Effects.Instantiate("Stairs Before Blobmother", transform);
    } else if (downstairs.floor.depth == 22) {
      PrefabCache.Effects.Instantiate("Stairs Before Fungal Colony", transform);
    }
  }
}
