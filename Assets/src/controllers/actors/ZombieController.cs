using UnityEngine;

public class ZombieController : ActorController {
  public Zombie zombie => (Zombie) actor;

  public override void Start() {
    base.Start();
    var sr = sprite.GetComponent<SpriteRenderer>();
    // copy the sprite
    var baseActorPrefab = FloorController.GetEntityPrefab(zombie.baseActor);
    sr.sprite = baseActorPrefab.GetComponentInChildren<SpriteRenderer>().sprite;
  }
}
