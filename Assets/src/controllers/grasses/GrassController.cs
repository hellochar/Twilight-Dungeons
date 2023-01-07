using System;
using UnityEngine;

public enum PulseType { Smaller, Larger }

public class GrassController : MonoBehaviour, IEntityController {
  [NonSerialized]
  public Grass grass;
  public PulseType pulses = PulseType.Smaller;

  // Start is called before the first frame update
  public virtual void Start() {
    this.transform.position = Util.withZ(this.grass.pos, this.transform.position.z);
    grass.OnNoteworthyAction += HandleNoteworthyAction;
  }

  private void HandleNoteworthyAction() {
    // when the grass's step does a noteworthy action
    if (GameModel.main.turnManager.activeEntity == grass && grass.isVisible) {
      GameModel.main.turnManager.forceStaggerThisTurn = true;
    }
    if (GetComponent<GrowAtStart>() == null) {
      var pulse = gameObject.AddComponent<PulseAnimation>();
      if (pulse != null) {
        pulse.pulseScale = pulses == PulseType.Smaller ? 0.75f : 1.25f;
      }
    }
  }

  void Update() {
    if (grass.readyToExpand && Mathf.Abs(transform.localScale.x - 1) < 0.01f) {
      if (GetComponent<PulseAnimation>() == null) {
        gameObject.AddComponent<PulseAnimation>().pulseScale = 0.9f;
      }
    }
  }
}
