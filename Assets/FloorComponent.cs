using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Adds and removes Tile prefabs to match the state of a Floor variable.
 */
public class FloorComponent : MonoBehaviour
{

    public Floor floor;
    private GameObject wallPrefab;
    private GameObject groundPrefab;
    
    // Start is called before the first frame update
    void Start()
    {
        wallPrefab = Resources.Load<GameObject>("WallTile");
        groundPrefab = Resources.Load<GameObject>("GroundTile");
        this.instantiateGameObjectsToMatchFloor();
    }

    void instantiateGameObjectsToMatchFloor() {
        for (int x = 0; x < floor.width; x++) {
            for (int y = 0; y < floor.height; y++) {
                Tile tile = floor.tiles[x, y];
                if (tile != null) {
                    Vector3Int pos = new Vector3Int(tile.pos.x, tile.pos.y, 0);
                    if (tile is Ground) {
                        Instantiate(groundPrefab, pos, Quaternion.identity, this.transform);
                    } else if (tile is Wall) {
                        Instantiate(wallPrefab, pos, Quaternion.identity, this.transform);
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
