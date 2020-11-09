using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchGameModel : MonoBehaviour {
  GameModel model;
  private GameObject floorPrefab;

  private Dictionary<Floor, MatchFloorState> floorComponents = new Dictionary<Floor, MatchFloorState>();

  private MatchFloorState currentFloorComponent;

  // Start is called before the first frame update
  void Start() {
    this.model = GameModel.main;
    this.floorPrefab = Resources.Load<GameObject>("Floor");
    currentFloorComponent = GetOrCreateFloorComponent(model.currentFloor);
    Player player = model.player;
    player.OnSetPlayerAction += HandleSetPlayerAction;
  }

  private Coroutine gameLoop;
  public void HandleSetPlayerAction(ActorAction action) {
    if (gameLoop == null && action != null) {
      gameLoop = StartCoroutine(model.StepUntilPlayerChoice(HandleGameLoopEnd));
    }
  }

  private void HandleGameLoopEnd() {
    gameLoop = null;
  }

  private MatchFloorState GetOrCreateFloorComponent(Floor floor) {
    if (!floorComponents.ContainsKey(floor)) {
      GameObject floorInstance = Instantiate(floorPrefab);
      MatchFloorState floorComponent = floorInstance.GetComponent<MatchFloorState>();
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

      MatchFloorState newFloorComponent = GetOrCreateFloorComponent(model.currentFloor);
      newFloorComponent.gameObject.SetActive(true);
      currentFloorComponent = newFloorComponent;
    }
  }
}
