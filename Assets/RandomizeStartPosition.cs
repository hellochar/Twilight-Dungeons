using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeStartPosition : MonoBehaviour {
  public Vector3 amount = new Vector3();

  void Start() {
    Vector3 offset = Random.insideUnitSphere;
    offset.Scale(amount);
    this.transform.position += offset;
  }
}
