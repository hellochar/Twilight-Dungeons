using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Adds and removes Tile prefabs to match the state of a Floor variable.
public class MatchFloorState : MonoBehaviour {

  public Floor floor;
  private Dictionary<System.Type, GameObject> prefabs;

  // Start is called before the first frame update
  void Start() {
    prefabs = new Dictionary<System.Type, GameObject>();
    System.Type[] types = new System.Type[] {
      // Tiles
      typeof(Ground), typeof(Wall), typeof(Downstairs), typeof(Upstairs), typeof(Dirt),
      // Plants
      typeof(BerryBush),
      // Enemies
      typeof(Bat)
    };
    foreach (System.Type t in types) {
      string resourceName = t.Name;
      prefabs.Add(t, Resources.Load<GameObject>(resourceName));
    }
    this.instantiateGameObjectsToMatchFloor();
  }

  void instantiateGameObjectsToMatchFloor() {
    for (int x = 0; x < floor.width; x++) {
      for (int y = 0; y < floor.height; y++) {
        Tile tile = floor.tiles[x, y];
        if (tile != null) {
          InstantiateGameObjectForEntity(tile);
        }
      }
    }
    foreach (Actor actor in floor.Actors()) {
      InstantiateGameObjectForEntity(actor);
    }
  }

  private void InstantiateGameObjectForEntity(Entity entity) {
    GameObject prefab;
    Vector3Int pos = new Vector3Int(entity.pos.x, entity.pos.y, 0);
    /// Player is Instantiated separately; not responsible here
    if (entity is Player) {
      return;
    }
    if (this.prefabs.TryGetValue(entity.GetType(), out prefab)) {
      GameObject gameObject = Instantiate(prefab, pos, Quaternion.identity, this.transform);
      if (entity is Tile) {
        gameObject.GetComponent<MatchTileState>().owner = (Tile) entity;
      } else if (entity is BerryBush) {
        gameObject.GetComponent<MatchPlantState>().plant = (BerryBush) entity;
      } else if (entity is Bat) {
        gameObject.GetComponent<MatchActorPosition>().actor = (Bat) entity;
      }
    } else {
      Debug.LogError($"Couldn't find prefab for {entity.GetType()}");
    }
  }

  void Update() {
    // TODO delete and add new game objects as the board changes
  }
}
