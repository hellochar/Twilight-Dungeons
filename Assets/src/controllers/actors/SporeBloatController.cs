public class SporeBloatController : ActorController {
  public override void Start() {
    base.Start();
    actor.OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    var explosion = PrefabCache.Effects.Instantiate("SporeBloat Explosion", transform);
    explosion.transform.parent = null;
  }
}
