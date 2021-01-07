using UnityEngine;

public enum PulseType { Smaller, Larger }

public class GrassController : MonoBehaviour, IEntityController {
  public Grass grass;
  public PulseType pulses = PulseType.Smaller;

  // Start is called before the first frame update
  public virtual void Start() {
    this.transform.position = Util.withZ(this.grass.pos, this.transform.position.z);
    grass.OnNoteworthyAction += HandleNoteworthyAction;
  }

  private void HandleNoteworthyAction() {
    if (GetComponent<GrowAtStart>() == null) {
      var pulse = gameObject.AddComponent<PulseAnimation>();
      if (pulse != null) {
        pulse.pulseScale = pulses == PulseType.Smaller ? 0.75f : 1.25f;
      }
    }
  }
}
