using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CampfireController : StationController {
  public Campfire campfire => (Campfire) body;
  public GameObject fire;

  public override void Start() {
    base.Start();
    campfire.OnHealed += HandleCampfireHealed;
  }

  void OnDestroy() {
    campfire.OnHealed -= HandleCampfireHealed;
  }

  private void HandleCampfireHealed() {
    gameObject.AddComponent<PulseAnimation>().pulseScale = 1.5f;
    var player = FloorController.current.GameObjectFor(GameModel.main.player);
    StartCoroutine(FireHealedAnimation(player));
  }

  private IEnumerator FireHealedAnimation(GameObject player) {
    var child = Instantiate(fire, player.transform);
    child.SetActive(true);
    yield return new WaitForSeconds(0.3f);
    child = Instantiate(fire, player.transform);
    child.SetActive(true);
    yield return new WaitForSeconds(0.3f);
    child = Instantiate(fire, player.transform);
    child.SetActive(true);
  }

  // public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
  //   Player player = GameModel.main.player;
  //   if (body.IsDead) {
  //     return base.GetPlayerInteraction(pointerEventData); // don't do anything to dead actors
  //   }
  //   return new SetTasksPlayerInteraction(
  //     new ChaseTargetTask(player, body),
  //     new GenericPlayerTask(player, () => {
  //       campfire.Heal();
  //     })
  //   );
  // }
}
