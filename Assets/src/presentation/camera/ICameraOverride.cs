public interface ICameraOverride {
  public CameraState overrideState { get; }
}

public class CameraFocuser : ICameraOverride {
  public Entity target;

  public CameraFocuser(Entity target) {
    this.target = target;
  }

  public CameraState overrideState => new CameraState {
    target = target
  };
}