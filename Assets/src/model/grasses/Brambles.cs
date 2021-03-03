using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Any creature walking over this takes 1 attack damage.")]
public class Brambles : Grass, IActorEnterHandler {
  public static bool CanOccupy(Tile tile) => tile is Ground;
  public Brambles(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor actor) {
    actor.TakeAttackDamage(1, actor);
    OnNoteworthyAction();
  }
}
