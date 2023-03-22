using System;
using UnityEngine;
// A Body that, after one day, turns into its inner Entity
[Serializable]
public class GrowingEntity : Body, IDaySteppable {
  public override Vector2Int[] shape => inner.shape;
  public Entity inner { get; private set; }
  public override string displayName => $"Growable {inner.displayName}";
  public override string description => $"Grows into a {inner.displayName} the next day.\n\n{inner.description}";

  public GrowingEntity(Vector2Int pos, Entity inner) : base(pos) {
    this.inner = inner;
  }

  public void StepDay() {
    var floor = this.floor;
    inner.pos = this.pos;
    KillSelf();
    floor.Put(inner);
  }
}
