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
    if (floor.bosses.Any()) {
      Shown("");
    } else if (floor.depth == 0 || floor.depth == 36) {
      Hidden();
    } else {
    switch (floor.EnemiesLeft()) {
      case 0:
        Shown("Cleared!");
        break;
      case var x when x > 3:
        Shown("Defeat all enemies.");
        break;
      case var x when x == 1:
        Shown("1 enemy left.");
        break;
      case var x:
        Shown($"{x} enemies left.");
        break;
      }
    }
  }

  void Hidden() {
    image.enabled = false;
    text.text = "";
  }

  void Shown(string t) {
    image.enabled = true;
    text.text = t;
  }
}
