using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SnailController : ActorController {
  protected override void HandleStatusAdded(Status status) {
    if(status is InShellStatus) {
      animator?.SetTrigger("GoingIntoShell");
    }
    base.HandleStatusAdded(status);
  }
}
