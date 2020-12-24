using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodBarController : MonoBehaviour {
  private RectTransform rectTransform;
  private GameObject barFilled;
  private Player player;
  void Start() {
    rectTransform = GetComponent<RectTransform>();
    barFilled = transform.Find("Bar_Filled").gameObject;
    player = GameModel.main.player;
  }

  void Update() {
    var fullness = player.fullness;
    var maxFullnessWidth = rectTransform.sizeDelta.x;
    var wantedWidth = fullness * maxFullnessWidth;
    RectTransform filledRectTransform = barFilled.GetComponent<RectTransform>();
    var sizeDelta = filledRectTransform.sizeDelta;
    filledRectTransform.sizeDelta = new Vector2(wantedWidth, sizeDelta.y);
  }
}
