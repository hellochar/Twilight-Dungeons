using System;

/// <summary>
/// Grants the Flying movement layer to the actor, allowing them to
/// traverse tiles that only block Walking (e.g. Chasms).
/// </summary>
[Serializable]
[ObjectInfo(description: "Can fly over chasms and other ground obstacles.")]
public class FlyingStatus : StackingStatus, IMovementLayerModifier {
  public override bool isDebuff => false;

  public FlyingStatus(int stacks) : base(stacks) {}
  public FlyingStatus() : base(1) {}

  public CollisionLayer Modify(CollisionLayer input) {
    return input | CollisionLayer.Flying;
  }
}
