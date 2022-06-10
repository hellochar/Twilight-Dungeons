using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AltarController : BodyController, ILongTapHandler {
  // public TeleportStone stone => (TeleportStone)actor;
  public new ParticleSystem particleSystem;

  public void HandleLongTap() {
    throw new NotImplementedException();
  }

  void Update() {
    var model = GameModel.main;
    particleSystem.gameObject.SetActive(model.permadeath);
  }

  public override void HandleInteracted(PointerEventData pointerEventData) {
    if (!GameModel.main.player.IsNextTo(body)) {
      MoveNextToTargetTask task = new MoveNextToTargetTask(GameModel.main.player, body.pos);
      GameModel.main.player.task = task;
      GameModel.main.turnManager.OnPlayersChoice += HandlePlayersChoice;
      return;
    } else {
      ShowAltarDialog();
    }
  }

  private async void HandlePlayersChoice() {
      GameModel.main.turnManager.OnPlayersChoice -= HandlePlayersChoice;
      if (GameModel.main.player.IsNextTo(body)) {
        await Task.Delay(100);
        ShowAltarDialog();
      }
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

    Popups.Create(
      "Altar",
      null,
      "Provides you immortality, if you choose it.",
      null,
      spriteGameObject,
      buttons: buttons
    );
  }
}
