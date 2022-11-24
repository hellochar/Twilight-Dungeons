using System;

[Serializable]
public class TimedEvent {
  public readonly float time;

  /// serialized by method name as a string. Don't use anonymous delegates. Don't rename method.
  public readonly Action action;
  public readonly Entity owner;
  public TimedEvent(Entity owner, float time, Action action) {
    this.owner = owner;
    this.time = time;
    this.action = action;
  }

  /// If the owner's floor is the current floor
  internal bool IsInvalid() => owner.floor != GameModel.main.currentFloor;

  public void UnregisterFromOwner() {
    owner.timedEvents.Remove(this);
  }
}
