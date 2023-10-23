using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AltarController : BodyController, IPopupOverride {
  public void HandleShowPopup() {
    ShowAltarDialog();
  }

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    return new SetTasksPlayerInteraction(
      new MoveNextToTargetTask(GameModel.main.player, body.pos),
      new GenericPlayerTask(GameModel.main.player, ShowAltarDialog)
    );
  }

  void ShowAltarDialog() {
    List<(string, Action)> buttons = new List<(string, Action)>();
    buttons.Add(("Destroy", () => {
      GameModel.main.permadeath = true;
      body.KillSelf();
    }));

    var spritePrefab = PrefabCache.UI.GetPrefabFor("Entity Image");
    var spriteGameObject = Instantiate(spritePrefab);
    var image = spriteGameObject.GetComponentInChildren<Image>();
    image.sprite = sprite.GetComponent<SpriteRenderer>().sprite;

    Popups.CreateStandard(
      "Altar to the Seven",
      null,
      "Lets you retry each level up to seven times.\n\nIf you're looking for a challenge, destroy the Altar.",
      null,
      spriteGameObject,
      buttons: buttons
    );
  }
}
