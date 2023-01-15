using System;
using UnityEngine;

public enum PulseType { Smaller, Larger }

public class GrassController : MonoBehaviour, IEntityController {
  [NonSerialized]
  public Grass grass;
  public PulseType pulses = PulseType.Smaller;

  public LineRenderer synergyLinePositive;
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

  private static Vector3[] zeroes = new Vector3[] {
    Vector3.zero,
    Vector3.zero, Vector3.zero,
    Vector3.zero, Vector3.zero,
    Vector3.zero, Vector3.zero,
    Vector3.zero, Vector3.zero
  };

  void Update() {
    // if (grass.readyToExpand && Mathf.Abs(transform.localScale.x - 1) < 0.01f) {
    //   if (GetComponent<PulseAnimation>() == null && GetComponent<GrowAtStart>() == null) {
    //     gameObject.AddComponent<PulseAnimation>().pulseScale = 0.9f;
    //   }
    // }

    if (grass.floor is HomeFloor) {
      synergyLinePositive.SetPositions(zeroes);
      if (grass.synergy.IsSatisfied(grass)) {
        int counter = 0;
        foreach(var offset in grass.synergy.offsets) {
          synergyLinePositive.SetPosition(1 + counter * 2, Util.withZ(offset));
          counter++;
        }
      }
    }
  }
}
