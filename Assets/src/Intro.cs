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
    if (Serializer.HasSaveOrCheckpoint()) {
      ShowNewGameConfirmation();
      return;
    }
    StartNewGame();
  }

  private void ShowNewGameConfirmation() {
    var popup = Popups.CreateStandard(
      title: "New Game",
      category: null,
      info: "Starting a new game will erase your current save.\n\nAre you sure?",
      flavor: null
    );

    // The popup was created without buttons, so the frame acts as a dismiss target.
    // Re-enable the Actions container and add our own buttons, since the standard
    // MakeButton flow depends on PlayerController which doesn't exist in this scene.
    var content = popup.content;

    var frameButton = content.GetComponent<Button>();
    if (frameButton != null) {
      Destroy(frameButton);
    }

    var actionsContainer = content.transform.Find("Actions");
    actionsContainer.gameObject.SetActive(true);
    content.transform.Find("Space").gameObject.SetActive(true);

    AddPopupButton("New Game", actionsContainer, popup.gameObject, () => StartNewGame());
    AddPopupButton("Cancel", actionsContainer, popup.gameObject, () => { });
  }

  private void AddPopupButton(string label, Transform parent, GameObject popup, Action action) {
    var button = Instantiate(PrefabCache.UI.GetPrefabFor("Action Button"), parent);
    button.GetComponentInChildren<TMPro.TMP_Text>().text = label;
    button.GetComponent<Button>().onClick.AddListener(() => {
      action();
      Destroy(popup);
    });
  }

  private void StartNewGame() {
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
