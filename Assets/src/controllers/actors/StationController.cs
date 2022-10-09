using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StationController : BodyController {
  public Station station => (Station) body;
  public ParticleSystem ps;
  public override void Start() {
    if (ps == null) {
      ps = GetComponentInChildren<ParticleSystem>();
    }
    base.Start();
    // GameModel.main.player.OnSetTask += HandleSetTask;
    // UpdatePS();
  }

  // void OnDestroy() {
  //   GameModel.main.player.OnSetTask -= HandleSetTask;
  // }

  private void HandleSetTask(ActorTask obj) {
    UpdatePS();
  }

  public void Update() {
    UpdatePS();
  }

  private void UpdatePS() {
    if (ps == null || station.IsDead) {
      return;
    }

    if (!station.isActive && !ps.isStopped) {
      ps.Stop();
    }
    else if (station.isActive && ps.isStopped) {
      ps.Play();
    }
  }

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (body.IsDead) {
      return base.GetPlayerInteraction(pointerEventData); // don't do anything to dead actors
    }
    
    return new SetTasksPlayerInteraction(
      new MoveNextToTargetTask(player, body.pos),
      new GenericPlayerTask(player, ShowDialog)
    );
  }

  void ShowDialog() {
    InteractionController.ShowPopupFor(body);
  }
}
