using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepLevelSpriteSwap : MonoBehaviour {
  private TileController tileController;
  public Sprite sprite12, sprite24;
  void Start() {
    tileController = GetComponent<TileController>();
    var generator = GameModel.main.generator;
    var floor = tileController.tile.floor;
    if (floor != null) {
      if (generator.EncounterGroup == generator.midGame) {
        GetComponent<SpriteRenderer>().sprite = sprite24;
      } else if (generator.EncounterGroup == generator.everything) {
        GetComponent<SpriteRenderer>().sprite = sprite12;
      }
    }
  }
}
