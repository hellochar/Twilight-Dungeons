using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchGameModel : MonoBehaviour {
  GameModel model;
  private GameObject floorPrefab;

  private Dictionary<Floor, FloorController> floorComponents = new Dictionary<Floor, FloorController>();

  private FloorController currentFloorComponent;

  // Start is called before the first frame update
  void Start() {
    this.model = GameModel.main;
    this.floorPrefab = Resources.Load<GameObject>("Floor");
    currentFloorComponent = GetOrCreateFloorComponent(model.currentFloor);
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
    if (!floorComponents.ContainsKey(floor)) {
      GameObject floorInstance = Instantiate(floorPrefab);
      FloorController floorComponent = floorInstance.GetComponent<FloorController>();
      floorComponent.floor = floor;
      floorComponents.Add(floor, floorComponent);
    }
    return floorComponents[floor];
  }

  // Update is called once per frame
  void Update() {
    // when the GameModel's current floor has changed, update the renderer to match
    if (model.currentFloor != currentFloorComponent.floor) {
      currentFloorComponent.gameObject.SetActive(false);

      FloorController newFloorComponent = GetOrCreateFloorComponent(model.currentFloor);
      newFloorComponent.gameObject.SetActive(true);
      currentFloorComponent = newFloorComponent;
    }
  }
}
