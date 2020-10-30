using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
  private GameObject floorPrefab;

  private Dictionary<Floor, FloorComponent> floorComponents = new Dictionary<Floor, FloorComponent>();

  private FloorComponent currentFloorComponent;

  // Start is called before the first frame update
  void Start() {
    this.floorPrefab = Resources.Load<GameObject>("Floor");
    currentFloorComponent = GetOrCreateFloorComponent(GameModel.main.currentFloor);
  }

  private FloorComponent GetOrCreateFloorComponent(Floor floor) {
    if (!floorComponents.ContainsKey(floor)) {
      GameObject floorInstance = Instantiate(floorPrefab);
      FloorComponent floorComponent = floorInstance.GetComponent<FloorComponent>();
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

      FloorComponent newFloorComponent = GetOrCreateFloorComponent(GameModel.main.currentFloor);
      newFloorComponent.gameObject.SetActive(true);
      currentFloorComponent = newFloorComponent;
    }
  }
}
