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
    foreach (Entity e in floor.entities) {
      GameObject prefab;
      Vector3Int pos = new Vector3Int(e.pos.x, e.pos.y, 0);
      // if (this.)
    }
  }

  private void InstantiateGameObjectForEntity(Tile tile) {
    GameObject prefab;
    Vector3Int pos = new Vector3Int(tile.pos.x, tile.pos.y, 0);
    if (this.prefabs.TryGetValue(tile.GetType(), out prefab)) {
      GameObject tileObject = Instantiate(prefab, pos, Quaternion.identity, this.transform);
      if (tile is Tile) {
        tileObject.GetComponent<MatchTileState>().owner = tile;
      }
    } else {
      Debug.LogError($"Couldn't find prefab for {tile.GetType()}");
    }
  }

  // Update is called once per frame
  void Update() {

  }
}
