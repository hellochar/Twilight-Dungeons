using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Take 1 attack damage when walking into Brambles.")]
public class Brambles : Grass, IActorEnterHandler {
  public static bool CanOccupy(Tile tile) => tile is Ground;
  public Brambles(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor actor) {
    actor.TakeAttackDamage(1, actor);
    OnNoteworthyAction();
  }
}
