using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterIndicatorController : MonoBehaviour {
  TMPro.TMP_Text text;
  void Start() {
    text = GetComponentInChildren<TMPro.TMP_Text>();
    text.text = GameModel.main.player.water.ToString();
  }

  void Update() {
    if (GameModel.main.player == null) {
      enabled = false;
      return;
    }
    if (int.TryParse(text.text, out var water)) {
      var nextNumber = Mathf.RoundToInt(Mathf.MoveTowards(water, GameModel.main.player.water, 1));
      text.text = nextNumber.ToString();
    } else {
      text.text = GameModel.main.player.water.ToString();
    }
  }

  public void HandleClicked() {
    Popups.CreateStandard(
      title: "Water",
      category: null,
      info: "Collect water from the caves.\nUse water for planting seeds back home.\n\nEvery 10 turns, 1 water evaporates.",
      flavor: "Water water everywhere...",
      sprite: transform.Find("Water Droplet").gameObject
    );
  }
}