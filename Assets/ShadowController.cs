using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowController : MonoBehaviour {
  public SpriteRenderer source;
  private SpriteRenderer shadow;

  // Start is called before the first frame update
  void Start() {
    shadow = GetComponent<SpriteRenderer>();
    Update();
  }

  // Update is called once per frame
  void Update() {
    if (shadow.sprite != source.sprite) {
      shadow.sprite = source.sprite;
    }
  }
}
