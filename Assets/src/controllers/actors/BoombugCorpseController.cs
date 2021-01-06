using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BoombugCorpseController : ActorController {
  BoombugCorpse corpse => (BoombugCorpse) actor;
  public override void Start() {
    base.Start();
    corpse.OnExploded += HandleExploded;
  }

  private void HandleExploded() {
    var explosion = PrefabCache.Effects.Instantiate("Boombug Explosion", transform);
    explosion.transform.parent = null;
  }
}
