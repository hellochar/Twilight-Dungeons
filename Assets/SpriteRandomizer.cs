using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteRandomizer : MonoBehaviour {
  public new SpriteRenderer renderer;
  public Sprite[] sprites;
  void Start() {
    if (renderer == null) {
      renderer = GetComponent<SpriteRenderer>();
    }
    renderer.sprite = Util.RandomPick(sprites);
  }
}
