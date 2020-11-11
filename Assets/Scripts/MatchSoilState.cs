using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class MatchSoilState : MatchTileState {
  public override void OnPointerClick(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    ItemSeed seed = (ItemSeed) player.inventory.ToList().Find(item => item is ItemSeed);
    if (seed != null) {
      var action = new MoveNextToTargetAction(player, owner.pos);
      action.OnDone += () => {
        // we must enqueue because OnDone gets called during the action block
        GameModel.main.EnqueueEvent(() => {
          player.action = new GenericAction(player, () => {
            seed.Plant((Soil) owner);
          });
        });
      };
      player.action = action;
    } else {
      base.OnPointerClick(pointerEventData);
    }
  }
}
