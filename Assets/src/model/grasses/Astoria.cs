using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Heals you for 4 HP on contact.\nConsumed on use.")]
public class Astoria : Grass, IActorEnterHandler {
  public Astoria(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor actor) {
    if (actor.hp < actor.maxHp && actor is Player) {
      actor.Heal(4);
      Kill(actor);
    }
  }
}