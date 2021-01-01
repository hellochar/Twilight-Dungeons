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
    text.text = "Depth " + (GameModel.main.currentFloor.depth);
    // text.text += "\nTime " + GameModel.main.time;
    // text.text += "\nStatuses: " + string.Join(", ", GameModel.main.player.statuses.list.Select(x => x.ToString()));
    // if (GameModel.main.turnManager != null) {
    //   this.tmpComponent.text += "\nTurn order: " + GameModel.main.turnManager.ToString();
    // }
  }
}
