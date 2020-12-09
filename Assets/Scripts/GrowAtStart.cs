using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrowAtStart : MonoBehaviour {
  private Vector3 initialScale;
  // Start is called before the first frame update
  void Start() {
    initialScale = transform.localScale;
    transform.localScale = transform.localScale * 0.01f;
  }

  // Update is called once per frame
  void Update() {
    if (Vector3.Distance(transform.localScale, initialScale) < 0.01f) {
      transform.localScale = initialScale;
      // we're done, remove ourselves
      Destroy(this);
    } else {
      transform.localScale = Vector3.Lerp(transform.localScale, initialScale, 10f * Time.deltaTime);
    }
  }
}
