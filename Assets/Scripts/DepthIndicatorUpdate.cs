using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthIndicatorUpdate : MonoBehaviour {
  private TMPro.TMP_Text tmpComponent;
  void Start() {
    this.tmpComponent = GetComponent<TMPro.TMP_Text>();
    this.tmpComponent.text = "";
  }

  // Update is called once per frame
  void Update() {
    int depth = GameModel.main.activeFloorIndex;
    this.tmpComponent.text = "Depth " + (depth + 1) + "\nTime " + GameModel.main.time;
    if (GameModel.main.turnManager != null) {
      this.tmpComponent.text += "\nTurn order: " + GameModel.main.turnManager.ToString();
    }
  }
}
