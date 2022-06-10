using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverHandler : MonoBehaviour {
  public GameObject[] uiToHide;
  public Image blackOverlay;
  public GameObject dPad;

  void Start() {
    GameModel.main.OnGameOver += HandleGameOver;
  }

  private void HandleGameOver(PlayStats stats) {
    if (stats.won) {
      var ezra = GameModel.main.currentFloor.bodies.First(b => b is Ezra) as Ezra;
      StartCoroutine(WinGameAnimation(ezra));
    } else {
      if (GameModel.main.permadeath) {
        #if !UNITY_EDITOR
        Serializer.DeleteSave0();
        Serializer.DeleteCheckpoint();
        #endif
        // player died
        InteractionController.isInputAllowed = false;
        dPad.SetActive(false);
        SceneManager.LoadSceneAsync("Scenes/GameOver", LoadSceneMode.Additive);
      } else {
        StartCoroutine(Transitions.GoToNewScene(this, blackOverlay, "Scenes/Game"));
      }
    }
  }

  IEnumerator WinGameAnimation(Actor ezra) {
    InteractionController.isInputAllowed = false;
    Camera.main.GetComponent<BoundCameraToFloor>().enabled = false;

    // hide UI
    foreach (var g in uiToHide) {
      g.SetActive(false);
    }

    StartCoroutine(Transitions.FadeAudio(Camera.main.GetComponentInChildren<AudioSource>(), 1, 0));
    var treeCenter = ezra.pos + Vector2Int.up * 9;
    yield return StartCoroutine(Transitions.ZoomAndPanCamera(11, treeCenter, 4));
    StartCoroutine(Transitions.ZoomAndPanCamera(11, treeCenter, 5));
    yield return StartCoroutine(Transitions.FadeImage(blackOverlay, Color.clear, Color.black, 5f));

    // show player and Ezra back at home
    var homePos = GameModel.main.home.center + Vector2Int.left * 2;
    GameModel.main.PutPlayerAt(0, homePos);
    ezra.statuses.RemoveOfType<SurprisedStatus>();
    ezra.ChangeFloors(GameModel.main.home, homePos + Vector2Int.right);

    while (FloorController.current.floor.depth != 0) {
      yield return new WaitForEndOfFrame();
    }
    yield return new WaitForEndOfFrame();
    
    var charmedPlayer = PrefabCache.Statuses.Instantiate("CharmedStatus", PlayerController.current.statuses.transform);
    charmedPlayer.GetComponent<StatusController>().enabled = false;

    var ezraGO = FloorController.current.GameObjectFor(ezra);
    var charmedEzra = PrefabCache.Statuses.Instantiate("CharmedStatus", ezraGO.transform.Find("Statuses"));
    charmedEzra.GetComponent<StatusController>().enabled = false;

    // show UI again
    foreach (var g in uiToHide) {
      g.SetActive(true);
    }

    // re-disable input since floor controllers changed
    InteractionController.isInputAllowed = false;
    // HACK hide dpad since there's a bug where it still thinks you're pressing it since
    // you pressed it to go to Ezra but it got inactivated so it doesn't register the
    // unpressed event. Also it looks bad in the final screen.
    dPad.SetActive(false);
    yield return new WaitForSeconds(1);

    #if !UNITY_EDITOR
    Serializer.DeleteSave0();
    Serializer.DeleteCheckpoint();
    #endif
    SceneManager.LoadSceneAsync("Scenes/GameOver", LoadSceneMode.Additive);
  }
}
