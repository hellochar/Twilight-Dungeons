using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
class PathfindingManager {
  /// if null, we need a recompute
  [NonSerialized] /// lazily instantiated
  private PathFind.Grid grid;
  private Floor floor;
  public PathfindingManager(Floor floor) {
    this.floor = floor;
    floor.OnEntityAdded += HandleEntityAdded;
    floor.OnEntityRemoved += HandleEntityRemoved;
  }

  [OnDeserialized]
  private void OnDeserialized() {
    floor.OnEntityAdded += HandleEntityAdded;
    floor.OnEntityRemoved += HandleEntityRemoved;
  }

  void HandleEntityAdded(Entity entity) {
    if (entity is Tile t) {
      grid = null;
    }
  }

  void HandleEntityRemoved(Entity entity) {
    if (entity is Tile tile) {
      grid = null;
    }
  }

  public List<Vector2Int> FindPathDynamic(Vector2Int pos, Vector2Int target, bool pretendTargetEmpty) {
    float[,] tilesmap = new float[floor.width, floor.height];
    for (int x = 0; x < floor.width; x++) {
      for (int y = 0; y < floor.height; y++) {
        Tile tile = floor.tiles[x, y];
        float weight = tile.GetPathfindingWeight();
        tilesmap[x, y] = weight;
      }
    }
    if (pretendTargetEmpty) {
      tilesmap[target.x, target.y] = 1f;
    }
    // every float in the array represent the cost of passing the tile at that position.
    // use 0.0f for blocking tiles.

    // create a grid
    PathFind.Grid grid = new PathFind.Grid(floor.width, floor.height, tilesmap);

    // create source and target points
    PathFind.Point _from = new PathFind.Point(pos.x, pos.y);
    PathFind.Point _to = new PathFind.Point(target.x, target.y);

    // get path
    // path will either be a list of Points (x, y), or an empty list if no path is found.
    List<PathFind.Point> path = PathFind.Pathfinding.FindPath(grid, _from, _to);
    return path.Select(p => new Vector2Int(p.x, p.y)).ToList();
  }

  /// find path around tiles; don't take actor bodies into account
  public List<Vector2Int> FindPathStatic(Vector2Int pos, Vector2Int target) {
    if (grid == null) {
      RecomputePathFindGrid();
    }
    // create source and target points
    PathFind.Point _from = new PathFind.Point(pos.x, pos.y);
    PathFind.Point _to = new PathFind.Point(target.x, target.y);

    // get path
    // path will either be a list of Points (x, y), or an empty list if no path is found.
    List<PathFind.Point> path = PathFind.Pathfinding.FindPath(grid, _from, _to);
    return path.Select(p => new Vector2Int(p.x, p.y)).ToList();
  }

  private void RecomputePathFindGrid() {
    float[,] tilesmap = new float[floor.width, floor.height];
    for (int x = 0; x < floor.width; x++) {
      for (int y = 0; y < floor.height; y++) {
        Tile tile = floor.tiles[x, y];
        float weight = tile.BasePathfindingWeight();
        tilesmap[x, y] = weight;
      }
    }
    // every float in the array represent the cost of passing the tile at that position.
    // use 0.0f for blocking tiles.

    // create a grid
    grid = new PathFind.Grid(floor.width, floor.height, tilesmap);
  }
}
