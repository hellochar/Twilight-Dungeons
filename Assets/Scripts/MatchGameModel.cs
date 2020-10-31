using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchGameModel : MonoBehaviour {
  private GameObject floorPrefab;

  private Dictionary<Floor, MatchFloorState> floorComponents = new Dictionary<Floor, MatchFloorState>();

  private MatchFloorState currentFloorComponent;

  // Start is called before the first frame update
  void Start() {
    this.floorPrefab = Resources.Load<GameObject>("Floor");
    currentFloorComponent = GetOrCreateFloorComponent(GameModel.main.currentFloor);
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
    if (GameModel.main.currentFloor != currentFloorComponent.floor) {
      currentFloorComponent.gameObject.SetActive(false);

      MatchFloorState newFloorComponent = GetOrCreateFloorComponent(GameModel.main.currentFloor);
      newFloorComponent.gameObject.SetActive(true);
      currentFloorComponent = newFloorComponent;
    }
  }
}
