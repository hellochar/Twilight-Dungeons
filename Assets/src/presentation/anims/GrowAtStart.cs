using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrowAtStart : MonoBehaviour {
  static float ANIMATION_TIME = 0.25f;
  private Vector3 initialScale;
  private float startTime;
  // Start is called before the first frame update
  void Start() {
    initialScale = transform.localScale;
    transform.localScale = transform.localScale * 0.01f;
  }

  // Update is called once per frame
  void Update() {
    var t = (Time.time - startTime) / ANIMATION_TIME;
    if (t >= 1) {
      transform.localScale = initialScale;
      // we're done, remove ourselves
      Destroy(this);
      return;
    }
    float lerpAmount = 1 - 1f / Mathf.Exp(t * Mathf.PI * 2);
    transform.localScale = Vector3.Lerp(transform.localScale, initialScale, lerpAmount);
  }
}
