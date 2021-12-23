using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusController : MonoBehaviour {
  [NonSerialized]
  public Status status;
  public virtual void Start() {
    if (status.list == null) {
      HandleRemoved();
    } else {
      status.OnRemoved += HandleRemoved;
    }
  }

  protected virtual void HandleRemoved() {
    Destroy(gameObject);
  }
}
