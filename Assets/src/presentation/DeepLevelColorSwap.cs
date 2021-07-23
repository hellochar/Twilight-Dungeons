using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepLevelColorSwap : MonoBehaviour {
  private TileController tileController;
  public Color color12, color24;
  void Start() {
    tileController = GetComponentInParent<TileController>();
    if (tileController.tile.floor.depth > 24) {
      GetComponent<SpriteRenderer>().color = color24;
    } else if (tileController.tile.floor.depth > 12) {
      GetComponent<SpriteRenderer>().color = color12;
    }
  }
}
