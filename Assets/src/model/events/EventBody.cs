using System;
using UnityEngine;

/// <summary>
/// A generic in-world entity that triggers an arbitrary NarrativeEvent when the player
/// steps on it. Extends Grass so it sits on a tile without blocking movement or occupying
/// the body slot. The EventBody is just a shell — all logic lives in the NarrativeEvent class.
/// </summary>
[Serializable]
[ObjectInfo(description: "Something unusual.")]
public class EventBody : Grass, IActorEnterHandler {
  public NarrativeEvent narrativeEvent;

  public EventBody(Vector2Int pos, NarrativeEvent evt) : base(pos) {
    this.narrativeEvent = evt;
  }

  public override string displayName => narrativeEvent.Title;

  public override string description => narrativeEvent.Description;

  public void HandleActorEnter(Actor who) {
    if (who is Player player) {
      var ctx = EventContext.ForEntity(this);
      narrativeEvent.Present(ctx);
    }
  }
}
