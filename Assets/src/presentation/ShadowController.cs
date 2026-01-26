using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowController : MonoBehaviour {
  public SpriteRenderer source;
  private SpriteRenderer shadow;

  void OnValidate() {
    Update();
  }

  // Start is called before the first frame update
  void Start() {
    Update();
  }

  // Update is called once per frame
  void Update() {
    if (shadow == null) {
      shadow = GetComponent<SpriteRenderer>();
    }
    if (source == null) {
      source = GetComponentInParent<SpriteRenderer>();
    }

    if (shadow.sprite != source.sprite) {
      shadow.sprite = source.sprite;
    }
  }
}
