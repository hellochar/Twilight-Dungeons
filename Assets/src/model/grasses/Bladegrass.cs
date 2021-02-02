using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Walk over to sharpen.\nOnce sharpened, any creature walking into this Bladegrass takes 2 damage and kills it.")]
public class Bladegrass : Grass, IActorEnterHandler, IActorLeaveHandler {
  public static bool CanOccupy(Tile tile) => tile is Ground;
  public bool isSharp = false;
  [field:NonSerialized]
  public event Action OnSharpened;
  public Bladegrass(Vector2Int pos) : base(pos) {}

  public void Sharpen() {
    if (!isSharp) {
      isSharp = true;
      OnSharpened?.Invoke();
      AddTimedEvent(10, Kill);
    }
  }

  public void HandleActorLeave(Actor obj) {
    Sharpen();
  }

  public void HandleActorEnter(Actor actor) {
    if (isSharp) {
      Kill();
      actor.TakeDamage(2);
    }
  }
}
