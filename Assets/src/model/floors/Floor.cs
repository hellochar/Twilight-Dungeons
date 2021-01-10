using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Floor {
  private int m_depth = 0;
  public int depth => m_depth;

  /// TODO refactor this into "layers": Tile Layer, Floor Layer, Main layer.
  public StaticEntityGrid<Tile> tiles;
  public StaticEntityGrid<Grass> grasses;
  public StaticEntityGrid<ItemOnGround> items;
  public MovingEntityList<Actor> actors;
  public HashSet<Entity> entities;
  public List<ISteppable> steppableEntities;


  public event Action<Entity> OnEntityAdded;
  public event Action<Entity> OnEntityRemoved;

  /// min inclusive, max exclusive in terms of map width/height
  public Vector2Int boundsMin => new Vector2Int(0, 0);
  public Vector2Int boundsMax => new Vector2Int(width, height);
  public Vector2 center => new Vector2(width / 2.0f, height / 2.0f);

  /// abstract bsp root
  internal Room root;
  /// all rooms (terminal bsp nodes). Sorted by 
  internal List<Room> rooms;
  internal Room upstairsRoom;
  internal Room downstairsRoom;

  public Upstairs upstairs {
    get {
      foreach (Tile t in this.tiles) {
        if (t is Upstairs) {
          return (Upstairs)t;
        }
      }
      return null;
    }
  }
  public Downstairs downstairs {
    get {
      foreach (Tile t in this.tiles) {
        if (t is Downstairs) {
          return (Downstairs)t;
        }
      }
      return null;
    }
  }

  private PathfindingManager pathfindingManager;

  public readonly int width;

  public readonly int height;

  public Floor(int depth, int width, int height) {
    this.m_depth = depth;
    this.width = width;
    this.height = height;
    this.tiles = new StaticEntityGrid<Tile>(this);
    this.grasses = new StaticEntityGrid<Grass>(this);
    this.items = new StaticEntityGrid<ItemOnGround>(this, (item) => {
      var newPosition = BreadthFirstSearch(item.pos, (_) => true)
        .Where(ItemOnGround.CanOccupy)
        .First()
        .pos;
      item.pos = newPosition;
    });
    this.actors = new MovingEntityList<Actor>(this);
    this.entities = new HashSet<Entity>();
    this.steppableEntities = new List<ISteppable>();
    pathfindingManager = new PathfindingManager(this);
  }

  internal void PutAll(IEnumerable<Entity> entities) {
    foreach (var entity in entities) {
      Put(entity);
    }
  }

  public void Put(Entity entity) {
    this.entities.Add(entity);

    if (entity is ISteppable s) {
      steppableEntities.Add(s);
    }

    if (entity is Tile tile) {
      tiles.Put(tile);
    } else if (entity is Actor actor) {
      actors.Put(actor);
    } else if (entity is Grass grass) {
      grasses.Put(grass);
    } else if (entity is ItemOnGround item) {
      items.Put(item);
    }

    /// HACK
    if (entity is IBlocksVision) {
      RecomputeVisiblity(GameModel.main.player);
    }

    entity.SetFloor(this);
    this.OnEntityAdded?.Invoke(entity);
  }

  /// Sets all terminal room connections by checking every pair of rooms if they're directly connected:
  /// find a path between rooms A and B
  /// If each tile on the path only belongs to A or B (or no room), then they're directly connected
  internal void ComputeConnectivity() {
    throw new NotImplementedException();
  }

  public void Remove(Entity entity) {
    if (!entities.Contains(entity)) {
      Debug.LogError("Removing " + entity + " from a floor it doesn't live in!");
    }
    this.entities.Remove(entity);

    if (entity is ISteppable s) {
      steppableEntities.Remove(s);
    }

    if (entity is Tile tile) {
      tiles.Remove(tile);
    } else if (entity is Actor a) {
      actors.Remove(a);
    } else if (entity is Grass g) {
      grasses.Remove(g);
    } else if (entity is ItemOnGround item) {
      items.Remove(item);
    }

    /// HACK
    if (entity is IBlocksVision) {
      RecomputeVisiblity(GameModel.main.player);
    }

    entity.SetFloor(null);
    this.OnEntityRemoved?.Invoke(entity);
  }

  private float lastStepTime = 0;

  internal void RecordLastStepTime(float time) {
    lastStepTime = time;
  }

  internal IEnumerable<Actor> AdjacentActors(Vector2Int pos) {
    return GetAdjacentTiles(pos).Select(x => x.actor).Where(x => x != null);
  }

  internal List<Actor> ActorsInCircle(Vector2Int center, int radius) {
    var actors = new List<Actor>();
    foreach (var pos in EnumerateCircle(center, radius)) {
      if (tiles[pos] != null && tiles[pos].actor != null) {
        actors.Add(tiles[pos].actor);
      }
    }
    return actors;
  }

  /// returns a list of adjacent positions that form the path, or an empty list if no path is found
  /// if pretendTargetEmpty is true, override the target tile to be walkable. You can then trim off
  /// the very end.
  internal List<Vector2Int> FindPath(Vector2Int pos, Vector2Int target, bool pretendTargetEmpty = false) {
    // return pathfindingManager.FindPathStatic(pos, target);
    return pathfindingManager.FindPathDynamic(pos, target, pretendTargetEmpty);
  }

  internal void CatchUpStep(float time) {
    foreach (var s in steppableEntities) {
      s.CatchUpStep(lastStepTime, time);
    }
  }

  internal void RemoveVisibility(Actor entity) {
    foreach (var pos in EnumerateCircle(entity.pos, entity.visibilityRange)) {
      Tile t = tiles[pos.x, pos.y];
      if (t.visibility == TileVisiblity.Visible) {
        t.visibility = TileVisiblity.Explored;
      }
    }
  }

  void RecomputeVisiblity(Actor entity) {
    if (entity != null && entity.floor == this) {
      RemoveVisibility(entity);
      AddVisibility(entity);
    }
  }

  internal void AddVisibility(Actor entity) {
    foreach (var pos in EnumerateCircle(entity.pos, entity.visibilityRange)) {
      Tile t = tiles[pos.x, pos.y];
      bool isVisible = TestVisibility(entity.pos, pos);
      if (isVisible) {
        t.visibility = TileVisiblity.Visible;
      }
    }
  }

  internal void ForceAddVisibility(IEnumerable<Vector2Int> positions) {
    foreach (var pos in positions) {
      Tile t = tiles[pos];
      t.visibility = TileVisiblity.Visible;
    }
  }

  /// returns true if the points have line of sight to each other
  public bool TestVisibility(Vector2Int source, Vector2Int end) {
    bool isVisible = true;
    if (tiles[end.x, end.y].ObstructsVision()) {
      var possibleEnds = GetAdjacentTiles(end).Where(tile => !tile.ObstructsVision()).OrderBy((tile) => {
        return Vector2Int.Distance(source, tile.pos);
      });
      /// find the closest neighbor that doesn't obstruct vision and go off that
      end = possibleEnds.FirstOrDefault()?.pos ?? end;
    }
    foreach (var pos in EnumerateLine(source, end)) {
      if (pos == source || pos == end) {
        continue;
      }
      Tile t = tiles[pos.x, pos.y];
      isVisible = isVisible && !t.ObstructsVision();
    }
    return isVisible;
  }

  /// includes the tile *at* the pos.
  public List<Tile> GetAdjacentTiles(Vector2Int pos) {
    List<Tile> list = new List<Tile>();
    int xMin = Mathf.Clamp(pos.x - 1, 0, width - 1);
    int xMax = Mathf.Clamp(pos.x + 1, 0, width - 1);
    int yMin = Mathf.Clamp(pos.y - 1, 0, height - 1);
    int yMax = Mathf.Clamp(pos.y + 1, 0, height - 1);
    for (int x = xMin; x <= xMax; x++) {
      for (int y = yMin; y <= yMax; y++) {
        list.Add(tiles[x, y]);
      }
    }
    return list;
  }

  public IEnumerable<Tile> GetCardinalNeighbors(Vector2Int pos) {
    var up = pos + new Vector2Int(0, +1);
    if (InBounds(up)) {
      yield return tiles[up];
    }

    var right = pos + new Vector2Int(+1, 0);
    if (InBounds(right)) {
      yield return tiles[right];
    }

    var down = pos + new Vector2Int(0, -1);
    if (InBounds(down)) {
      yield return tiles[down];
    }

    var left = pos + new Vector2Int(-1, 0);
    if (InBounds(left)) {
      yield return tiles[left];
    }
  }

  public bool InBounds(Vector2Int pos) {
    return pos.x >= 0 && pos.y >= 0 && pos.x < width && pos.y < height;
  }

  public IEnumerable<Vector2Int> EnumerateCircle(Vector2Int center, float radius) {
    Vector2Int extent = new Vector2Int(Mathf.CeilToInt(radius), Mathf.CeilToInt(radius));
    foreach (var pos in EnumerateRectangle(center - extent, center + extent)) {
      if (Vector2Int.Distance(pos, center) <= radius) {
        yield return pos;
      }
    }
  }

  /// max is exclusive
  public IEnumerable<Vector2Int> EnumerateRectangle(Vector2Int min, Vector2Int max) {
    min = Vector2Int.Max(min, boundsMin);
    max = Vector2Int.Min(max, boundsMax);
    for (int x = min.x; x < max.x; x++) {
      for (int y = min.y; y < max.y; y++) {
        yield return new Vector2Int(x, y);
      }
    }
  }

  public IEnumerable<Vector2Int> EnumeratePerimeter() {
    for (int x = 0; x < width; x++) {
      yield return new Vector2Int(x, 0);
      yield return new Vector2Int(x, height - 1);
    }
    for (int y = 0; y < height; y++) {
      yield return new Vector2Int(0, y);
      yield return new Vector2Int(width - 1, y);
    }
  }

  public IEnumerable<Tile> EnumerateRoomTiles(Room room, int extrude = 0) {
    return EnumerateRoom(room, extrude).Select(x => tiles[x]);
  }

  public IEnumerable<Vector2Int> EnumerateRoom(Room room, int extrude = 0) {
    Vector2Int extrudeVector = new Vector2Int(extrude, extrude);
    return EnumerateRectangle(room.min - extrudeVector, room.max + new Vector2Int(1, 1) + extrudeVector);
  }


  public IEnumerable<Vector2Int> EnumerateFloor() {
    return this.EnumerateRectangle(boundsMin, boundsMax);
  }

  /// always starts right on the startPoint, and always ends right on the endPoint
  public IEnumerable<Vector2Int> EnumerateLine(Vector2Int startPoint, Vector2Int endPoint) {
    Vector2 offset = endPoint - startPoint;
    for (float t = 0; t <= offset.magnitude; t += 0.5f) {
      Vector2 point = startPoint + offset.normalized * t;
      Vector2Int p = new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
      yield return p;
    }
    yield return endPoint;
  }

  public IEnumerable<Tile> BreadthFirstSearch(Vector2Int startPos, Func<Tile, bool> predicate, bool randomizeNeighborOrder = true) {
    LinkedList<Tile> frontier = new LinkedList<Tile>();
    HashSet<Tile> seen = new HashSet<Tile>();
    frontier.AddFirst(tiles[startPos]);
    while (frontier.Any()) {
      var tile = frontier.First.Value;
      frontier.RemoveFirst();
      yield return tile;
      seen.Add(tile);
      var adjacent = GetCardinalNeighbors(tile.pos).Except(seen).Where(predicate).ToList();
      if (randomizeNeighborOrder) {
        adjacent.Shuffle();
      }
      foreach (var next in adjacent) {
        frontier.AddLast(next);
      }
    }
  }

  public void PlaceUpstairs(Vector2Int pos, bool addWalls = true) {
    Put(new Upstairs(pos));
    // surround sides with wall, but ensure right tile is open
    if (addWalls) {
      Put(new Wall(pos + new Vector2Int(-1, -1)));
      Put(new Wall(pos + new Vector2Int(-1, 0)));
      Put(new Wall(pos + new Vector2Int(-1, 1)));

      Put(new Wall(pos + new Vector2Int(0, -1)));
      Put(new Wall(pos + new Vector2Int(0, 1)));

      Put(new Ground(pos + new Vector2Int(1, 0)));
    }
  }

  public void PlaceDownstairs(Vector2Int pos, bool addWalls = true) {
    Put(new Downstairs(pos));
    // surround sides with wall, but ensure left tile is open
    if (addWalls) {
      Put(new Ground(pos + new Vector2Int(-1, 0)));

      Put(new Wall(pos + new Vector2Int(0, -1)));
      Put(new Wall(pos + new Vector2Int(0, 1)));

      Put(new Wall(pos + new Vector2Int(1, -1)));
      Put(new Wall(pos + new Vector2Int(1, 0)));
      Put(new Wall(pos + new Vector2Int(1, 1)));
    }
  }

}

class PathfindingManager {
  /// if null, we need a recompute
  private PathFind.Grid grid;
  private Floor floor;
  public PathfindingManager(Floor floor) {
    this.floor = floor;
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