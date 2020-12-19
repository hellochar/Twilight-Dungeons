using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusController : MonoBehaviour {
  public Status status;
  void Start() {
    status.OnRemoved += HandleRemoved;
  }

  private void HandleRemoved() {
    Destroy(this.gameObject);
  }
}
