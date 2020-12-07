using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchStatusState : MonoBehaviour {
  public Status status;
  public Animator animator;
  void Start() {
    status.OnRemoved += HandleRemoved;
    animator = transform.parent.GetComponentInChildren<Animator>();
    animator?.SetBool("StuckStatus", true);
  }

  private void HandleRemoved() {
    animator?.SetBool("StuckStatus", false);
    Destroy(this.gameObject);
  }
}
