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
      PrefabCache.Effects.Instantiate("Stair Decoration", transform);
    }
  }

  public override void HandleInteracted(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    player.SetTasks(
      new MoveNextToTargetTask(player, tile.pos),
      new GenericPlayerTask(player, downstairs.TryGoDownstairs)
    );
  }
}
