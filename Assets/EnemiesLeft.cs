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
    var enemiesLeft = floor.EnemiesLeft();
    var enemiesText = (enemiesLeft > 3) ? "???" : enemiesLeft.ToString();
    text.text = floor.depth == 0 ? "" : $"{floor.EnemiesLeft()} enemies left.";
  }
}
