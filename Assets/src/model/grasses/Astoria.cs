using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Heals 4 HP on contact.\nConsumed on use.")]
public class Astoria : Grass, IActorEnterHandler {
  public Astoria(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor actor) {
    actor.Heal(4);
    Kill(actor);
  }
}