using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepLevelColorSwap : MonoBehaviour {
  private TileController tileController;
  public Color color12, color24;
  void Start() {
    tileController = GetComponentInParent<TileController>();
    var generator = GameModel.main.generator;
    if (generator.EncounterGroup == generator.midGame) {
      GetComponent<SpriteRenderer>().color = color24;
    } else if (generator.EncounterGroup == generator.everything) {
      GetComponent<SpriteRenderer>().color = color12;
    }
  }
}
