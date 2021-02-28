using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BumpAndReturn : MonoBehaviour {
  static float ANIMATION_TIME = 0.25f;
  public float intensity = 0.75f;
  public Vector3 target;

  private Vector3 startLocal;
  private Vector3 targetLocal;
  private float startTime;
  private Animator animator;

  // Start is called before the first frame update
  void Start() {
    startLocal = transform.localPosition;
    targetLocal = transform.InverseTransformPoint(target);
    startTime = Time.time;
    /// override it temporarily
    animator = GetComponent<Animator>();
    if (animator != null) {
      animator.enabled = false;
    }
  }

  // Update is called once per frame
  void Update() {
    var t = (Time.time - startTime) / ANIMATION_TIME;
    if (t >= 1) {
      transform.localPosition = startLocal;
      if (animator != null) {
        animator.enabled = true;
      }
      Destroy(this);
      return;
    }
    float lerp = Mathf.Pow(Mathf.Cos(Mathf.PI / 2 + Mathf.PI * Mathf.Sqrt(t)), 4) * intensity;
    transform.localPosition = Vector3.Lerp(startLocal, targetLocal, lerp);
  }
}
