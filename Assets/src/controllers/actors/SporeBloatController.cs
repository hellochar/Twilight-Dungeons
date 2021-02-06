using UnityEngine;

public class SporeBloatController : ActorController {
  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);

    var explosion = PrefabCache.Effects.Instantiate("SporeBloat Explosion", transform);
    explosion.transform.parent = null;
  }
}
