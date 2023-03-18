using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TunnelrootController : GrassController {
  public Tunnelroot tunnelroot => (Tunnelroot) grass;
  public Sprite closed, middle, open;

  // Start is called before the first frame update
  public override void Start() {
    tunnelroot.OnOpenChanged += HandleOpenChanged;
    HandleOpenChanged(tunnelroot.IsOpen());
  }

  private void HandleOpenChanged(bool isOpen) {
    if (isOpen && sr.sprite != open) {
      StartCoroutine(Transitions.SpriteSwap(sr, 0.5f, closed, middle, open));
    } else if (!isOpen && sr.sprite != closed) {
      StartCoroutine(Transitions.SpriteSwap(sr, 0.5f, open, middle, closed));
    }
  }
}
