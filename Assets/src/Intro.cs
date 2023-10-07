using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Intro : MonoBehaviour {
  public Image blackOverlay;
  public AudioClip playerMove;
  public FullpageNarrativeController prologue;

  void Awake() {
    // unset current game.
    GameModel.main = null;
    UnityEngine.CrashReportHandler.CrashReportHandler.enableCaptureExceptions = !Application.isEditor;
    Application.targetFrameRate = Screen.currentResolution.refreshRate;
  }

  void Start() {
    if (!Serializer.HasSaveOrCheckpoint()) {
      transform.Find("Continue").gameObject.SetActive(false);

      // maybe jump into tutorial immediately
      if (!TutorialController.HasFinishedTutorial()) {
        StartIntroAndTutorial();
      }
    }
  }

  // hooked up to settings button
  public void ReplayIntroAndTutorial() => StartIntroAndTutorial();

  private void StartIntroAndTutorial() {
    StartCoroutine(prologue.PlayNarrative(() => {
      GameModel.GenerateTutorialAndSetMain();
      GoToGameScene();
    }));
  }

  public void NewGame() {
    StartCoroutine(WalkPlayer());
    FadeOutButtonsAndMusic();
    try {
      GameModel.GenerateNewGameAndSetMain();
      GoToGameScene();
    } catch (Exception e) {
      ShowExceptionPopup(e);
    }
  }
  
  public void ShowExceptionPopup(Exception e) {
    var popup = Popups.CreateStandard(
      title: "Error",
      category: null,
      info: "Sorry, something went wrong! Please screenshot this and send it to hellocharlien@hotmail.com.",
      flavor: null,
      errorText: e.Message == "" ? e.GetType().Name : e.Message);
    var controller = popup.GetComponent<PopupController>();
    controller.OnClose += () => SceneManager.LoadSceneAsync("Scenes/Intro");
    Debug.LogException(e);
  }

  public async void Continue() {
    StartCoroutine(WalkPlayer());
    FadeOutButtonsAndMusic();
    try {
      await Task.Run(() => GameModel.main = Serializer.LoadSave0());
      GoToGameScene();
    } catch (Exception e) {
      ShowExceptionPopup(e);
    }
  }

  private IEnumerator WalkPlayer() {
    GameObject player = GameObject.Find("Player");
    Vector3 target = new Vector3(6.5f, -0.5f, 0);
    while (Vector3.Distance(player.transform.position, target) > Mathf.Epsilon) {
      Vector3 currentTarget = player.transform.position;
      currentTarget.x += 1;
      // playerMove.Play(0.5f);
      while (Vector3.Distance(player.transform.position, currentTarget) > .005f) {
        player.transform.position = Vector3.Lerp(player.transform.position, currentTarget, 20 * Time.deltaTime);
        yield return new WaitForEndOfFrame();
      }
      player.transform.position = currentTarget;
      // yield return new WaitForSecondsRealtime(TurnManager.GAME_TIME_TO_SECONDS_WAIT_SCALE);
      yield return new WaitForSeconds(.2f);
    }
  }

  private void FadeOutButtonsAndMusic() {
    StartCoroutine(Transitions.FadeAudio(Camera.main.GetComponent<AudioSource>(), 1, 0));
    foreach (var button in GetComponentsInChildren<Button>()) {
      button.interactable = false;
    }
  }

  public void GoToGameScene() {
    StartCoroutine(Transitions.GoToNewScene(this, blackOverlay, "Scenes/Game"));
  }
}
