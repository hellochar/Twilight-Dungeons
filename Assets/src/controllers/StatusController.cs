using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusController : MonoBehaviour {
  static string ACTOR_SPRITENAME = "Sprite";
  public Status status;
  public Animator animator;
  void Start() {
    status.OnRemoved += HandleRemoved;
    // this is supposed to be the Actor's Animator
    animator = transform.parent.Find(ACTOR_SPRITENAME).GetComponentInChildren<Animator>();
    animator?.SetBool("StuckStatus", true);
  }

  private void HandleRemoved() {
    animator?.SetBool("StuckStatus", false);
    Destroy(this.gameObject);
  }
}
