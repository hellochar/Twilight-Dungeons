using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameModelController : MonoBehaviour {
  public Image overlay;
  public static GameModelController main;
  [NonSerialized]
  GameModel model;
  private GameObject floorPrefab;
  private Coroutine gameLoop;
  public bool isPlayersChoice => gameLoop == null;

  private Dictionary<Floor, FloorController> floorControllers = new Dictionary<Floor, FloorController>();

  private FloorController currentFloorController;
  public FloorController CurrentFloorController => currentFloorController;

  void Awake() {
    UnityEngine.CrashReportHandler.CrashReportHandler.enableCaptureExceptions = !Application.isEditor;
    Application.targetFrameRate = Screen.currentResolution.refreshRate;
    #if UNITY_EDITOR
    // GameModel.GenerateTutorialAndSetMain();
    if (GameModel.main == null) {
      if (Serializer.HasSave()) {
        GameModel.main = Serializer.LoadSave0();
        if (GameModel.main.CheckObviouslyBroken()) {
          GameModel.main.TryRecoverFromBrokenState();
        }
      } else {
        GameModel.GenerateNewGameAndSetMain();
      }
    }
    #endif
    this.model = GameModel.main;
    this.floorPrefab = Resources.Load<GameObject>("Floor");
    main = this;
  }

  // Start is called before the first frame update
  void Start() {
    currentFloorController = GetOrCreateFloorController(model.currentFloor);
    Player player = model.player;
    player.OnSetTask += HandleSetPlayerTask;
    model.turnManager.OnPlayersChoice += HandlePlayersChoice;
    model.turnManager.OnPlayerCannotPerform += HandlePlayerCannotPerform;
  }


#if !UNITY_EDITOR
  void OnApplicationFocus(bool hasFocus) {
    /// save
    if (!hasFocus) {
      Serializer.SaveMainToFile();
    }
  }

  void OnApplicationPause(bool isPaused) {
    if (isPaused) {
      Serializer.SaveMainToFile();
    }
  }

  /// We cannot save on quit because it's not guaranteed our process will have the grace period
  /// to finish writing to disk; this risks corrupting the save. OnApplicationPause should cover
  /// us.
  // void OnApplicationQuit() {
  //   Serializer.SaveMainToFile();
  // }
#endif

  public void HandleSetPlayerTask(ActorTask action) {
    if (isPlayersChoice && action != null) {
      gameLoop = StartCoroutine(model.StepUntilPlayerChoice());
    }
  }

  [NonSerialized]
  public bool isSteppingDay = false;
  public void GoNextDay() {
    isSteppingDay = true;
    // CameraFocuser focuser = new CameraFocuser(GameModel.main.player);
    CameraFocuser focuser = new CameraFocuser(GameModel.main.home.entities.OfType<Pit>().First());
    CameraController.main.SetCameraOverride(focuser);

    // var messageGameObject = PrefabCache.UI.Instantiate("Map Selector Message");
    // messageGameObject.transform.SetParent(GameObject.Find("Canvas").transform, false);
    // var textComponent = messageGameObject.GetComponentInChildren<TMPro.TMP_Text>();
    // textComponent.text = "While you were away...";

    gameLoop = StartCoroutine(model.StepDay(
      // handle entity turn
      (Entity e) => {
        // textComponent.text = $"{e.displayName}'s Turn";
        var gameObject = CurrentFloorController.GameObjectFor(e);
        // gameObject might be null if we're loading the game from scratch and the Floor GameObjects haven't been created yet
        var pulse = gameObject?.AddComponent<PulseAnimation>();
        if (pulse != null) {
          pulse.pulseScale = 0.75f;
        }
        // focuser.target = e;
      },

      // on StepDay finished
      () => {
        gameLoop = null;
        isSteppingDay = false;
        CameraController.main.SetCameraOverride(null);
        // Destroy(messageGameObject);
      }
    ));
  }

  private void HandlePlayersChoice() {
    gameLoop = null;
  }

  private void HandlePlayerCannotPerform(CannotPerformActionException e) {
    if (e.Message != "") {
      Debug.LogWarning(e.Message);
      Messages.Create(e.Message);
      AudioClipStore.main.uiError.Play();
    }
  }

  private FloorController GetOrCreateFloorController(Floor floor) {
    if (!floorControllers.ContainsKey(floor)) {
      GameObject instance = Instantiate(floorPrefab);
      FloorController controller;
      if (floor is TutorialFloor) {
        controller = instance.AddComponent<TutorialFloorController>();
      } else {
        controller = instance.AddComponent<FloorController>();
      }
      controller.floor = floor;
      floorControllers.Add(floor, controller);
      /// hack - play floor animation
      CameraController.main.PlayFloorZoomAnimation();
    }
    return floorControllers[floor];
  }

  private bool isTransitioningBetweenHome = false;
  // Update is called once per frame
  void Update() {
    // when the GameModel's current floor has changed, update the renderer to match
    if (model.currentFloor != currentFloorController.floor && !isTransitioningBetweenHome) {
      int newDepth = model.currentFloor.depth;
      int oldDepth = currentFloorController.floor.depth;
      if (oldDepth == 0 || newDepth == 0) {
        // we teleported; do a slow animation
        StartCoroutine(TransitionBetweenHomeFloor());
      } else {
        // we're never going to visit this depth again; destroy it
        Destroy(currentFloorController.gameObject);
        ActivateNewFloor(model.currentFloor);
      }
    }
  }

  void ActivateNewFloor(Floor floor) {
    var newFloorController = GetOrCreateFloorController(floor);
    newFloorController.gameObject.SetActive(true);
    currentFloorController = newFloorController;
  }

  IEnumerator TransitionBetweenHomeFloor() {
    isTransitioningBetweenHome = true;
    // deactivate home or cave
    currentFloorController.gameObject.SetActive(false);

    // add black overlay
    var player = GameObject.Find("Player");
    player.SetActive(false);
    yield return StartCoroutine(Transitions.FadeImage(overlay, Color.clear, Color.black, 0.5f));
    yield return new WaitForSeconds(0.5f);
    ActivateNewFloor(model.currentFloor);
    player.SetActive(true);
    isTransitioningBetweenHome = false;
    // don't block
    StartCoroutine(Transitions.FadeImage(overlay, Color.black, Color.clear, 0.5f));
  }
}
