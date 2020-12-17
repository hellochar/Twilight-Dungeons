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
    var name = status.GetType().Name;
    animator?.SetBool(name, true);
  }

  private void HandleRemoved() {
    var name = status.GetType().Name;
    animator?.SetBool(name, false);
    Destroy(this.gameObject);
  }
}
