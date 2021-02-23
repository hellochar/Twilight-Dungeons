using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameModelController : MonoBehaviour {
  public static GameModelController main;
  [NonSerialized]
  GameModel model;
  private GameObject floorPrefab;

  private Dictionary<Floor, FloorController> floorControllers = new Dictionary<Floor, FloorController>();

  private FloorController currentFloorController;
  public FloorController CurrentFloorController => currentFloorController;

  void Awake() {
    #if UNITY_EDITOR
    if (GameModel.main == null) {
      if (Serializer.HasSave()) {
        GameModel.main = Serializer.LoadFromFile();
      } else {
        GameModel.GenerateNewGameAndSetMain();
      }
      // GameModel.GenerateTutorialAndSetMain();
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

  private Coroutine gameLoop;
  public void HandleSetPlayerTask(ActorTask action) {
    if (gameLoop == null && action != null) {
      gameLoop = StartCoroutine(model.StepUntilPlayerChoice());
    }
  }

  private void HandlePlayersChoice() {
    gameLoop = null;
  }

  private void HandlePlayerCannotPerform(CannotPerformActionException e) {
    Debug.LogWarning(e.Message);
    Messages.Create(e.Message);
    AudioClipStore.main.uiError.Play();
  }

  private FloorController GetOrCreateFloorController(Floor floor) {
    if (!floorControllers.ContainsKey(floor)) {
      GameObject instance = Instantiate(floorPrefab);
      FloorController controller = instance.GetComponent<FloorController>();
      if (floor is TutorialFloor) {
        Destroy(controller);
        controller = instance.AddComponent<TutorialFloorController>();
      }
      controller.floor = floor;
      floorControllers.Add(floor, controller);
    }
    return floorControllers[floor];
  }

  private bool isTransitioning = false;
  // Update is called once per frame
  void Update() {
    // when the GameModel's current floor has changed, update the renderer to match
    if (model.currentFloor != currentFloorController.floor && !isTransitioning) {
      var depthDifference = Math.Abs(model.currentFloor.depth - currentFloorController.floor.depth);
      if (depthDifference > 1) {
        isTransitioning = true;
        // we teleported; do a slow animation
        StartCoroutine(TransitionFloorSlow());
      } else {
        DeactivateCurrentFloorController();
        ActivateNewFloor(model.currentFloor);
      }
    }
  }

  void DeactivateCurrentFloorController() {
    currentFloorController.gameObject.SetActive(false);
  }

  void ActivateNewFloor(Floor floor) {
    var newFloorController = GetOrCreateFloorController(floor);
    newFloorController.gameObject.SetActive(true);
    currentFloorController = newFloorController;
  }

  IEnumerator TransitionFloorSlow() {
    DeactivateCurrentFloorController();
    // add black overlay
    var overlay = PrefabCache.UI.Instantiate("BlackOverlay", GameObject.Find("Canvas").transform);
    var img = overlay.GetComponent<Image>();
    img.color = new Color(0, 0, 0, 0);
    var player = GameObject.Find("Player");
    player.SetActive(false);
    yield return StartCoroutine(Transitions.FadeTo(img, 0.5f));
    yield return new WaitForSeconds(0.5f);
    ActivateNewFloor(model.currentFloor);
    player.SetActive(true);
    isTransitioning = false;
    var ftd = overlay.AddComponent<FadeThenDestroy>();
    ftd.shrink = 0;
    ftd.fadeTime = 0.5f;
  }
}
