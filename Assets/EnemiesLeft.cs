using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EnemiesLeft : MonoBehaviour {
  private TMPro.TMP_Text text;
  private Image image;
  void Start() {
    text = transform.Find("Text").GetComponent<TMPro.TMP_Text>();
    text.text = "";
    image = GetComponent<Image>();
  }

  // Update is called once per frame
  void Update() {
    var floor = GameModel.main.currentFloor;
    image.enabled = floor.depth != 0;
    switch (floor.EnemiesLeft()) {
      case var x when floor.depth == 0:
        text.text = "";
        break;
      case 0:
        text.text = "Cleared!";
        break;
      case var x when x > 3:
        text.text = "Defeat all enemies.";
        break;
      case var x:
        text.text = $"{x} enemies left.";
        break;
    }
  }
}
