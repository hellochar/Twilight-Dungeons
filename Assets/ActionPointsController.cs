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
  }

  void OnDestroy() {
    GameModel.main.OnPlayerChangeFloor -= HandlePlayerChangeFloor;
  }

  private void HandlePlayerChangeFloor(Floor arg1, Floor arg2) {
    gameObject.SetActive(GameModel.main.currentFloor == GameModel.main.home);
  }

  // Update is called once per frame
  void Update() {
    var points = GameModel.main.home.actionPoints;
    var pointsMax = GameModel.main.home.maxActionPoints;
    text.text = $"Actions: {points}/{pointsMax}";
  }
}
