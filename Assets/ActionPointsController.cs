using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionPointsController : MonoBehaviour {
  TMPro.TMP_Text text;
  // Start is called before the first frame update
  void Start() {
    text = GetComponent<TMPro.TMP_Text>();
    GameModel.main.OnPlayerChangeFloor += HandlePlayerChangeFloor;
    HandlePlayerChangeFloor(null, null);
#if !experimental_actionpoints
    Destroy();
    return;
#endif
  }

  void OnDestroy() {
    GameModel.main.OnPlayerChangeFloor -= HandlePlayerChangeFloor;
  }

  private void HandlePlayerChangeFloor(Floor arg1, Floor arg2) {
    gameObject.SetActive(GameModel.main.currentFloor == GameModel.main.home);
  }

  // Update is called once per frame
  void Update() {
#if experimental_actionpoints
    var points = GameModel.main.player.actionPoints;
    var pointsMax = GameModel.main.player.maxActionPoints;
    text.text = $"Actions: {points}/{pointsMax}";
#endif
  }
}
