using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TunnelrootController : GrassController {
  public Tunnelroot tunnelroot => (Tunnelroot) grass;
  public Sprite closed, middle, open;
  SpriteRenderer sr;

  // Start is called before the first frame update
  public override void Start() {
    sr = GetComponent<SpriteRenderer>();
    tunnelroot.OnOpenChanged += HandleOpenChanged;
    HandleOpenChanged(tunnelroot.IsOpen());
  }

  private void HandleOpenChanged(bool isOpen) {
    if (isOpen && sr.sprite != open) {
      StartCoroutine(TransitionSprite(closed, middle, open));
    } else if (!isOpen && sr.sprite != closed) {
      StartCoroutine(TransitionSprite(open, middle, closed));
    }
  }

  public IEnumerator TransitionSprite(Sprite start, Sprite mid, Sprite end) {
    sr.sprite = start;
    yield return new WaitForSeconds(.13f);
    sr.sprite = mid;
    yield return new WaitForSeconds(.13f);
    sr.sprite = end;
  }
}
