using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PulseAnimation : MonoBehaviour {
  public float FADE_TIME = 0.33f;
  public float pulseScale = 0.75f;
  private Vector3 currentScale;
  private float startTime;
  // Start is called before the first frame update
  void Start() {
    currentScale = transform.localScale;
    startTime = Time.time;
  }

  public void Larger() {
    pulseScale = 1.25f;
  }

  public void Scale(float v) {
    pulseScale = v;
  }

  void Update() {
    var t = (Time.time - startTime) / FADE_TIME;
    if (t >= 1) {
      transform.localScale = currentScale;
      Destroy(this);
      return;
    }
    float scalar = Util.MapLinear(Mathf.Pow(Mathf.Cos(t * Mathf.PI), 4), 1, 0, 1, pulseScale);
    transform.localScale = currentScale * scalar;
  }
}
