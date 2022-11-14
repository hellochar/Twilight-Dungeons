using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AltarController : BodyController, IPopupOverride {
  public new ParticleSystem particleSystem;

  void Update() {
    var model = GameModel.main;
    particleSystem.gameObject.SetActive(model.permadeath);
  }

  void ShowAltarDialog() {
    List<(string, Action)> buttons = new List<(string, Action)>();
    if (GameModel.main.permadeath) {
      buttons.Add(("Disable Permadeath", () => {
        GameModel.main.permadeath = false;
      }));
    } else {
      buttons.Add(("Enable Permadeath", () => {
        GameModel.main.permadeath = true;
      }));
    }
    buttons.Add(("Back", () => {}));

    var spritePrefab = PrefabCache.UI.GetPrefabFor("Entity Image");
    var spriteGameObject = Instantiate(spritePrefab);
    var image = spriteGameObject.GetComponentInChildren<Image>();
    image.sprite = sprite.GetComponent<SpriteRenderer>().sprite;

    Popups.CreateStandard(
      "Altar",
      null,
      "Provides you immortality, if you choose it.",
      null,
      spriteGameObject,
      buttons: buttons
    );
  }

  public void HandleShowPopup() {
    ShowAltarDialog();
  }
}
