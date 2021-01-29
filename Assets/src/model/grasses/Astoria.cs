using UnityEngine;

[ObjectInfo(description: "Heals 4 HP on contact.\nConsumed on use.")]
[System.Serializable]
public class Astoria : Grass, IActorEnterHandler {
  public Astoria(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor actor) {
    actor.Heal(4);
    Kill();
  }
}