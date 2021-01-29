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
    GameModel.InitOrLoadMain();
    this.model = GameModel.main;
    this.floorPrefab = Resources.Load<GameObject>("Floor");
    SceneManager.sceneUnloaded += HandleSceneUnloaded;
    main = this;
  }

  private void HandleSceneUnloaded(Scene arg0) {
    // Serializer.SaveToPlayerPrefs(this.model);
  }

  // Start is called before the first frame update
  void Start() {
    currentFloorController = GetOrCreateFloorController(model.currentFloor);
    Player player = model.player;
    player.OnSetTask += HandleSetPlayerTask;
    model.turnManager.OnPlayersChoice += HandlePlayersChoice;
    model.turnManager.OnPlayerCannotPerform += HandlePlayerCannotPerform;
    // AudioClipStore.main.gameStart.Play();
  }

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
      GameObject floorInstance = Instantiate(floorPrefab);
      FloorController floorComponent = floorInstance.GetComponent<FloorController>();
      floorComponent.floor = floor;
      floorControllers.Add(floor, floorComponent);
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
    yield return StartCoroutine(LoadMainScene.FadeToBlack(img, 0.5f));
    yield return new WaitForSeconds(0.5f);
    ActivateNewFloor(model.currentFloor);
    player.SetActive(true);
    isTransitioning = false;
    var ftd = overlay.AddComponent<FadeThenDestroy>();
    ftd.shrink = 0;
    ftd.fadeTime = 0.5f;
  }
}
