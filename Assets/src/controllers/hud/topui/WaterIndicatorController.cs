using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterIndicatorController : MonoBehaviour {
  TMPro.TMP_Text text;
  void Start() {
    text = GetComponentInChildren<TMPro.TMP_Text>();
  }

  void Update() {
    text.text = GameModel.main.player.water.ToString("0.##");
  }
}