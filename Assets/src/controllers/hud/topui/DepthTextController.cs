using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DepthTextController : MonoBehaviour {
  private TMPro.TMP_Text text;
  void Start() {
    text = transform.Find("Text").GetComponent<TMPro.TMP_Text>();
    text.text = "";
  }

  // Update is called once per frame
  void Update() {
    if (GameModel.main.IsTransient()) {
      text.text = "Turn " + GameModel.main.time;
    } else {
      text.text = "Depth " + (GameModel.main.currentFloor.depth) + "\nTurn " + GameModel.main.time;
    }
  }

  public void ShowPopup() {
    var playTime = TimeSpan.FromSeconds(Time.timeSinceLevelLoad).ToString(@"hh\:mm\:ss");
    var info = $"Playtime {playTime}\nSeed " + GameModel.main.seed.ToString("x");
    Popups.Create(
      title: null,
      category: "",
      info: info,
      flavor: "",
      errorText: GameModel.main.turnManager.latestException?.ToString()
    );
  }
}
