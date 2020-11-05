using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Adds and removes Tile prefabs to match the state of a Floor variable.
public class MatchFloorState : MonoBehaviour {

  public Floor floor;
  private Dictionary<System.Type, GameObject> prefabs = new Dictionary<System.Type, GameObject>();
  private Dictionary<Entity, GameObject> gameObjectMap = new Dictionary<Entity, GameObject>();

  // Start is called before the first frame update
  void Start() {
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
    // TODO do the same thing for tile changes
    floor.OnActorAdded.AddListener(HandleActorAdded);
    floor.OnActorRemoved.AddListener(HandleActorRemoved);
  }

  void HandleActorAdded(Actor a) {
    InstantiateGameObjectForEntity(a);
  }

  void HandleActorRemoved(Actor a) {
    GameObject currentObject = gameObjectMap[a];
    if (currentObject == null) {
      Debug.LogWarning("" + a + " was removed from floor " + floor + " but didn't have a GameObject.");
    }
    Destroy(currentObject);
    gameObjectMap.Remove(a);
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
    if (gameObjectMap.ContainsKey(entity)) {
      throw new System.Exception("Creating gameObject for entity that already has one" + entity);
    }
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
      } else if (entity is Actor) {
        gameObject.GetComponent<MatchActorState>().actor = (Actor) entity;
      }
      gameObjectMap[entity] = gameObject;
    } else {
      Debug.LogError($"Couldn't find prefab for {entity.GetType()}");
    }
  }
}
