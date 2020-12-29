using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModelController : MonoBehaviour {
  public static GameModelController main;
  GameModel model;
  private GameObject floorPrefab;

  private Dictionary<Floor, FloorController> floorControllers = new Dictionary<Floor, FloorController>();

  private FloorController currentFloorController;
  public FloorController CurrentFloorController => currentFloorController;

  void Awake() {
    GameModel.InitMain();
    this.model = GameModel.main;
    this.floorPrefab = Resources.Load<GameObject>("Floor");
    main = this;
  }

  // Start is called before the first frame update
  void Start() {
    currentFloorController = GetOrCreateFloorComponent(model.currentFloor);
    Player player = model.player;
    player.OnSetTask += HandleSetPlayerTask;
    model.turnManager.OnPlayersChoice += HandlePlayersChoice;
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

  private FloorController GetOrCreateFloorComponent(Floor floor) {
    if (!floorControllers.ContainsKey(floor)) {
      GameObject floorInstance = Instantiate(floorPrefab);
      FloorController floorComponent = floorInstance.GetComponent<FloorController>();
      floorComponent.floor = floor;
      floorControllers.Add(floor, floorComponent);
    }
    return floorControllers[floor];
  }

  // Update is called once per frame
  void Update() {
    // when the GameModel's current floor has changed, update the renderer to match
    if (model.currentFloor != currentFloorController.floor) {
      currentFloorController.gameObject.SetActive(false);

      FloorController newFloorComponent = GetOrCreateFloorComponent(model.currentFloor);
      newFloorComponent.gameObject.SetActive(true);
      currentFloorController = newFloorComponent;
    }
  }
}
