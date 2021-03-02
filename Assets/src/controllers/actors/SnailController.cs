using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SnailController : ActorController {
  public Sprite normal, inShell33, inShell66, inShell;
  public override void Start() {
    base.Start();
  }

  public override void HandleStatusAdded(Status status) {
    if(status is InShellStatus) {
      var sr = sprite.GetComponent<SpriteRenderer>();
      StartCoroutine(Transitions.SpriteSwap(sr, 0.4f, normal, inShell33, inShell66, inShell));
    }
    base.HandleStatusAdded(status);
  }

  public override void HandleStatusRemoved(Status status) {
    if (status is InShellStatus) {
      var sr = sprite.GetComponent<SpriteRenderer>();
      StartCoroutine(Transitions.SpriteSwap(sr, 0.4f, inShell, inShell66, inShell33, normal));
    }
    base.HandleStatusRemoved(status);
  }
}
