using UnityEngine;

public class FungalSentinelController : ActorController {
  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);

    var explosion = PrefabCache.Effects.Instantiate("Fungal Sentinel Explosion", transform);
    explosion.transform.parent = null;
  }
}
