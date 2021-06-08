using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchPosition : MonoBehaviour {
  public GameObject target;
  void Start() {
    LateUpdate();   
  }

  void LateUpdate() {
    transform.localPosition = target.transform.localPosition;
  }
}
