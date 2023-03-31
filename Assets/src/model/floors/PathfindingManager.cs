using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
class PathfindingManager {
  [NonSerialized] /// re-created on deserialization
  List<Tile> openSet = new List<Tile>();
  [NonSerialized] /// re-created on deserialization
  HashSet<Tile> closedSet = new HashSet<Tile>();
  [NonSerialized] /// re-created on deserialization
  Dictionary<Tile, int> gCosts = new Dictionary<Tile, int>();
  [NonSerialized] /// re-created on deserialization
  Dictionary<Tile, int> hCosts = new Dictionary<Tile, int>();
  [NonSerialized] /// re-created on deserialization
  Dictionary<Tile, Tile> parents = new Dictionary<Tile, Tile>();
  private Floor floor;
  public PathfindingManager(Floor floor) {
    this.floor = floor;
  }

  [OnDeserialized]
  private void OnDeserialized() {
    openSet = new List<Tile>();
    closedSet = new HashSet<Tile>();
    gCosts = new Dictionary<Tile, int>();
    hCosts = new Dictionary<Tile, int>();
    parents = new Dictionary<Tile, Tile>();
  }

  public List<Vector2Int> FindPathDynamic(Vector2Int pos, Vector2Int target, bool pretendTargetEmpty = false, Func<Tile, float> weightFn = null) {
    var path = FindPathImpl(pos, target, pretendTargetEmpty, weightFn);
    if (path != null) {
      return path.Select(x => x.pos).ToList();
    }
    return new List<Vector2Int>();
  }

  private int gCost(Tile t) => gCosts.GetValueOrDefault(t);
  private int hCost(Tile t) => hCosts.GetValueOrDefault(t);
  private Tile parent(Tile t) => parents.GetValueOrDefault(t);
  private int fCost(Tile t) => +gCost(t) + hCost(t);

  private static readonly Func<Tile, float> DEFAULT_WEIGHT_FN = (Tile t) => t.GetPathfindingWeight();

  // adapted from https://github.com/RonenNess/Unity-2d-pathfinding
  /// pretendTargetEmpty - if true, the targetPos will be treated as walkable, even if it's not
  private List<Tile> FindPathImpl(Vector2Int startPos, Vector2Int targetPos, bool pretendTargetEmpty, Func<Tile, float> weightFn = null) {
    if (weightFn == null) {
      weightFn = DEFAULT_WEIGHT_FN;
    }
    if (!floor.InBounds(startPos) || !floor.InBounds(targetPos)) {
      throw new Exception("start or end out of bounds");
    }
    Tile startNode = floor.tiles[startPos];
    Tile targetNode = floor.tiles[targetPos];

    openSet.Clear();
    closedSet.Clear();
    gCosts.Clear();
    hCosts.Clear();
    parents.Clear();

    openSet.Add(startNode);

    bool isAtEmptyTarget(Tile t) => pretendTargetEmpty && t.pos == targetPos ? true : false;

    while (openSet.Count > 0) {
      Tile currentNode = openSet[0];
      for (int i = 1; i < openSet.Count; i++) {
        if (fCost(openSet[i]) < fCost(currentNode) || fCost(openSet[i]) == fCost(currentNode) && hCost(openSet[i]) < hCost(currentNode)) {
          currentNode = openSet[i];
        }
      }

      openSet.Remove(currentNode);
      closedSet.Add(currentNode);

      if (currentNode == targetNode) {
        return RetracePath(startNode, targetNode);
      }

      foreach (Tile neighbour in floor.GetAdjacentTiles(currentNode.pos)) {
        if (closedSet.Contains(neighbour)) {
          continue;
        }

        // make walking to the empty target tile cost 1
        var weight = isAtEmptyTarget(neighbour) ? 1 : weightFn.Invoke(neighbour);
        var canBeOccupied = weight != 0;
        if (!canBeOccupied) {
          continue;
        }

        var neighbourWeight = (int)(10.0f * weight);
        int newMovementCostToNeighbour = gCost(currentNode) + GetDistance(currentNode, neighbour) * neighbourWeight;
        if (newMovementCostToNeighbour < gCost(neighbour) || !openSet.Contains(neighbour)) {
          gCosts[neighbour] = newMovementCostToNeighbour;
          hCosts[neighbour] = GetDistance(neighbour, targetNode);
          parents[neighbour] = currentNode;

          if (!openSet.Contains(neighbour)) {
            openSet.Add(neighbour);
          }
        }
      }
    }

    return null;
  }

  private List<Tile> RetracePath(Tile startNode, Tile endNode) {
    List<Tile> path = new List<Tile>();
    Tile currentNode = endNode;

    while (currentNode != startNode) {
      path.Add(currentNode);
      currentNode = parent(currentNode);
    }
    path.Reverse();
    return path;
  }

  // speed optimized (ints only)
  private static int GetDistance(Tile nodeA, Tile nodeB) {
    int dstX = Mathf.Abs(nodeA.pos.x - nodeB.pos.x);
    int dstY = Mathf.Abs(nodeA.pos.y - nodeB.pos.y);

    if (dstX > dstY)
      return 14 * dstY + 10 * (dstX - dstY);
    return 14 * dstX + 10 * (dstY - dstX);
  }
}
