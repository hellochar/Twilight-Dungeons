using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BoombugCorpseController : ActorController {
  public override void Start() {
    base.Start();
    actor.OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    var explosion = PrefabCache.Effects.Instantiate("Boombug Explosion", transform);
    explosion.transform.parent = null;
  }
}
