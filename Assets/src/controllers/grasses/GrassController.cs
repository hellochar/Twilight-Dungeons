using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum PulseType { Smaller, Larger }

public class GrassController : MonoBehaviour, IEntityController, IPlayerInteractHandler, IOnTopActionHandler {
  [NonSerialized]
  public Grass grass;
  public PulseType pulses = PulseType.Smaller;

  public LineRenderer synergyLinePositive;

  protected SpriteRenderer sr;
  private Color startColor;
  public virtual void Start() {
    this.transform.position = Util.withZ(this.grass.pos, this.transform.position.z);
    grass.OnNoteworthyAction += HandleNoteworthyAction;
    if (synergyLinePositive == null) {
      synergyLinePositive = transform.Find("SynergyPositive").gameObject.GetComponent<LineRenderer>();
    }
    sr = GetComponent<SpriteRenderer>();
    startColor = sr.color;
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

  // void Update() {
  //   // if (grass.readyToExpand && Mathf.Abs(transform.localScale.x - 1) < 0.01f) {
  //   //   if (GetComponent<PulseAnimation>() == null && GetComponent<GrowAtStart>() == null) {
  //   //     gameObject.AddComponent<PulseAnimation>().pulseScale = 0.9f;
  //   //   }
  //   // }

  //   if (grass.floor is HomeFloor) {
  //     // if (!grass.readyToExpand) {
  //     //   Color gs = new Color(startColor.grayscale, startColor.grayscale, startColor.grayscale, startColor.a);
  //     //   Color transparent = gs;
  //     //   transparent.a = 0;
  //     //   sr.color = Color.Lerp(gs, transparent, Mathf.Pow(Mathf.Sin(Time.time * 2), 2));
  //     // } else {
  //     //   sr.color = startColor;
  //     // }
  //     synergyLinePositive.SetPositions(zeroes);
  //     if (grass.synergy.IsSatisfied(grass)) {
  //       int counter = 0;
  //       foreach(var offset in grass.synergy.offsets) {
  //         synergyLinePositive.SetPosition(1 + counter * 2, Util.withZ(offset));
  //         counter++;
  //       }
  //     }
  //   }
  // }

  public virtual PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    // if (grass.floor is HomeFloor) {
    //   return new SetTasksPlayerInteraction(
    //     new MoveNextToTargetTask(GameModel.main.player, grass.pos),
    //     new ShowInteractPopupTask(GameModel.main.player, grass)
    //   );
    // }
    return new SetTasksPlayerInteraction(
      new MoveToTargetTask(GameModel.main.player, grass.pos)
    );
  }

  public string OnTopActionName => "Harvest";

  public void HandleOnTopAction() {
    grass.Harvest(GameModel.main.player);
  }
}
