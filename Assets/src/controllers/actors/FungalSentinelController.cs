using System;
using UnityEngine;

public class FungalSentinelController : ActorController {
  public FungalSentinel sentinel => (FungalSentinel) actor;
  public override void Start() {
    base.Start();
    sentinel.OnExploded += HandleExploded;
  }

  void OnDestroy() {
    sentinel.OnExploded -= HandleExploded;
  }

  private void HandleExploded() {
    var explosion = PrefabCache.Effects.Instantiate("Fungal Sentinel Explosion", transform);
    explosion.transform.parent = null;
  }
}
