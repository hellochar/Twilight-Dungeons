using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BladegrassController : GrassController {

  public Bladegrass bladegrass => (Bladegrass) grass;

  public override void Start() {
    base.Start();
    bladegrass.OnSharpened += HandleSharpened;
    if (bladegrass.isSharp) {
      HandleSharpened();
    }
  }

  private void HandleSharpened() {
    GetComponent<Animator>()?.SetTrigger("Sharpened");
  }
}