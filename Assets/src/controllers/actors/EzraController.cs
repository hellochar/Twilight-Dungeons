using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EzraController : ActorController {
  public override void HandleInteracted(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (actor.IsDead) {
      return; // don't do anything to dead actors
    }
    if (player.IsNextTo(actor)) {
      // wake ezra up, win the game!!!
      actor.ClearTasks();
      actor.statuses.Add(new SurprisedStatus());
      StartCoroutine(WinGame());
    } else {
      player.task = new MoveNextToTargetTask(player, actor.pos);
    }
  }

  IEnumerator WinGame() {
    InteractionController.isInputAllowed = false;
    StartCoroutine(Transitions.FadeAudio(Camera.main.GetComponent<AudioSource>(), 1, 0));
    yield return StartCoroutine(ZoomInCamera());

    /// lasts 3 seconds
    var playerController = GameModelController.main.CurrentFloorController.GameObjectFor(GameModel.main.player).GetComponent<PlayerController>();
    playerController.ShowSpeechBubble();
    yield return new WaitForSeconds(3);

    ShowSpeechBubble();
    yield return new WaitForSeconds(3);

    playerController.ShowSpeechBubble();
    yield return new WaitForSeconds(3);

    ShowSpeechBubble();
    yield return new WaitForSeconds(3);

    playerController.actor.statuses.Add(new CharmedStatus());
    actor.statuses.Add(new CharmedStatus());

    yield return new WaitForSeconds(5);

    /// TODO play victory music
    var blackOverlay = GameObject.Find("BlackOverlay");
    yield return StartCoroutine(Transitions.FadeTo(blackOverlay.GetComponent<Image>(), 5));
  }

  IEnumerator ZoomInCamera() {
    var duration = 1;
    var start = Time.time;
    var t = 0f;
    var camera = Camera.main.GetComponent<Camera>();
    var startSize = camera.orthographicSize;
    do {
      t = (Time.time - start) / duration;
      camera.orthographicSize = EasingFunctions.EaseInQuad(startSize, 4, t);
      yield return new WaitForEndOfFrame();
    } while (t < 1);
  }
}