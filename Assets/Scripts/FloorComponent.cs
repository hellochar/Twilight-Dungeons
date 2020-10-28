using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Adds and removes Tile prefabs to match the state of a Floor variable.
 */
public class FloorComponent : MonoBehaviour {

  public Floor floor;
  private Dictionary<System.Type, GameObject> prefabs;

  // Start is called before the first frame update
  void Start() {
    prefabs = new Dictionary<System.Type, GameObject>();
    System.Type[] tileTypes = new System.Type[] { typeof(Ground), typeof(Wall), typeof(Downstairs), typeof(Upstairs) };
    foreach (System.Type t in tileTypes) {
      string resourceName = $"{t.Name}Tile";
      prefabs.Add(t, Resources.Load<GameObject>(resourceName));
    }
    this.instantiateGameObjectsToMatchFloor();
  }

  void instantiateGameObjectsToMatchFloor() {
    for (int x = 0; x < floor.width; x++) {
      for (int y = 0; y < floor.height; y++) {
        Tile tile = floor.tiles[x, y];
        if (tile != null) {
          GameObject prefab;
          Vector3Int pos = new Vector3Int(tile.pos.x, tile.pos.y, 0);
          if (this.prefabs.TryGetValue(tile.GetType(), out prefab)) {
            Instantiate(prefab, pos, Quaternion.identity, this.transform);
          } else {
            Debug.LogError($"Couldn't find prefab for {tile.GetType()}");
          }
        }
      }
    }
  }

  // Update is called once per frame
  void Update() {

  }
}
