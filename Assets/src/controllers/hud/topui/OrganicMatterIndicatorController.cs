using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrganicMatterIndicatorController : MonoBehaviour {
  TMPro.TMP_Text text;
  void Start() {
    text = GetComponentInChildren<TMPro.TMP_Text>();
    text.text = GameModel.main.player.organicMatter.ToString();
  }

  void Update() {
    if (GameModel.main.player == null) {
      enabled = false;
      return;
    }
    if (int.TryParse(text.text, out var organicMatter)) {
      var nextNumber = Mathf.RoundToInt(Mathf.MoveTowards(organicMatter, GameModel.main.player.organicMatter, 1));
      text.text = nextNumber.ToString();
    } else {
      text.text = GameModel.main.player.organicMatter.ToString();
    }
  }

  public void HandleClicked() {
    Popups.Create(
      title: "Organic Matter",
      category: null,
      info: "Recycle unused items into Organic Matter.\nUse Organic Matter for creating structures back home.",
      flavor: "Loam, compost, mulch, mold...",
      sprite: transform.Find("Organic Matter Icon").gameObject
    );
  }
}