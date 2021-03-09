using UnityEngine;

public class FruitingBodyController : ActorController {
  public ParticleSystem ps;
  public FruitingBody fb => (FruitingBody) actor;

  public override void Start() {
    base.Start();
    fb.OnSprayed += HandleSprayed;
  }

  void OnDestroyed() {
    fb.OnSprayed -= HandleSprayed;
  }

  public void HandleSprayed() {
    ps.Play();
  }
}
