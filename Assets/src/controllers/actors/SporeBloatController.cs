public class SporeBloatController : ActorController {
  public override void HandleDeath() {
    base.HandleDeath();
    var explosion = PrefabCache.Effects.Instantiate("SporeBloat Explosion", transform);
    explosion.transform.parent = null;
  }
}
