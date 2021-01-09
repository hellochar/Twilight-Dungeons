using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DepthTextController : MonoBehaviour {
  private TMPro.TMP_Text text;
  void Start() {
    text = GetComponent<TMPro.TMP_Text>();
    text.text = "";
  }

  // Update is called once per frame
  void Update() {
    var timeSpan = TimeSpan.FromSeconds(Time.timeSinceLevelLoad);
    var timeSpanText = timeSpan.ToString(@"hh\:mm\:ss");
    text.text = "Depth " + (GameModel.main.currentFloor.depth) + "\nTurn " + GameModel.main.time + "\n" + timeSpanText + "\nSeed " + GameModel.main.seed;
    // text.text += "\nTime " + GameModel.main.time;
    // text.text += "\nStatuses: " + string.Join(", ", GameModel.main.player.statuses.list.Select(x => x.ToString()));
    // if (GameModel.main.turnManager != null) {
    //   this.tmpComponent.text += "\nTurn order: " + GameModel.main.turnManager.ToString();
    // }
  }
}
