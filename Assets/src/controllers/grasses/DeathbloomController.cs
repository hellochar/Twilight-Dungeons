using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeathbloomController : GrassController {
  public Deathbloom deathbloom => (Deathbloom) grass;
  private GameObject small, large;

  // Start is called before the first frame update
  public override void Start() {
    small = transform.Find("Small").gameObject;
    large = transform.Find("Large").gameObject;
    large.SetActive(false);
    deathbloom.OnBloomed += HandleBloomed;
  }

  void HandleBloomed() {
    large.SetActive(true);
    small.SetActive(false);
  }
}
