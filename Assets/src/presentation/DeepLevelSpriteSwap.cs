using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepLevelSpriteSwap : MonoBehaviour {
  private TileController tileController;
  public Sprite sprite12, sprite24;
  void Start() {
    tileController = GetComponent<TileController>();
    if (tileController.tile.floor.depth > 24) {
      GetComponent<SpriteRenderer>().sprite = sprite24;
    } else if (tileController.tile.floor.depth > 12) {
      GetComponent<SpriteRenderer>().sprite = sprite12;
    }
  }
}