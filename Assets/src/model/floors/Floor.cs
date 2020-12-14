using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Floor {
  /// TODO refactor this into "layers": Tile Layer, Floor Layer, Main layer.
  public StaticEntityGrid<Tile> tiles;
  public StaticEntityGrid<Grass> grasses;
  public MovingEntityList<Actor> actors;


  public event Action<Entity> OnEntityAdded;
  public event Action<Entity> OnEntityRemoved;

  /// min inclusive, max exclusive in terms of map width/height
  public Vector2Int boundsMin => new Vector2Int(0, 0);
  public Vector2Int boundsMax => new Vector2Int(width, height);

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

  public readonly int width;

  public readonly int height;

  public Floor(int width, int height) {
    this.width = width;
    this.height = height;
    this.tiles = new StaticEntityGrid<Tile>(this);
    this.grasses = new StaticEntityGrid<Grass>(this);
    this.actors = new MovingEntityList<Actor>(this);
  }

  public void Put(Entity entity) {
    if (entity is Tile tile) {
      tiles.Put(tile);
    } else if (entity is Actor actor) {
      actors.Put(actor);
    } else if (entity is Grass grass) {
      grasses.Put(grass);
    } else {
      throw new Exception("Cannot add unrecognized entity " + entity);
    }
    entity.SetFloor(this);
    this.OnEntityAdded?.Invoke(entity);
  }

  public void Remove(Entity entity) {
    if (entity is Tile tile) {
      tiles.Remove(tile);
    } else if (entity is Actor a) {
      actors.Remove(a);
    } else if (entity is Grass g) {
      grasses.Remove(g);
    } else {
      throw new Exception("Cannot remove unrecognized entity " + entity);
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
    float[,] tilesmap = new float[width, height];
    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        Tile tile = tiles[x, y];
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
    PathFind.Grid grid = new PathFind.Grid(width, height, tilesmap);

    // create source and target points
    PathFind.Point _from = new PathFind.Point(pos.x, pos.y);
    PathFind.Point _to = new PathFind.Point(target.x, target.y);

    // get path
    // path will either be a list of Points (x, y), or an empty list if no path is found.
    List<PathFind.Point> path = PathFind.Pathfinding.FindPath(grid, _from, _to);
    return path.Select(p => new Vector2Int(p.x, p.y)).ToList();
  }

  internal IEnumerable<Actor> Actors() {
    foreach (Actor a in this.actors) {
      yield return a;
    }
  }

  internal IEnumerable<Grass> Grasses() {
    foreach (Grass g in this.grasses) {
      yield return g;
    }
  }


  internal void CatchUpStep(float time) {
    // step all actors until they're up to speed
    foreach (Actor a in actors) {
      a.CatchUpStep(lastStepTime, time);
    }
    foreach (Grass g in grasses) {
      g.CatchUpStep(lastStepTime, time);
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

  /// includes the tile *at* the pos
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

  public void PlaceUpstairs(Vector2Int pos) {
    // surround sides with wall, but ensure right tile is open
    Put(new Wall(pos + new Vector2Int(-1, -1)));
    Put(new Wall(pos + new Vector2Int(-1, 0)));
    Put(new Wall(pos + new Vector2Int(-1, 1)));

    Put(new Wall(pos + new Vector2Int(0, -1)));
    Put(new Upstairs(pos));
    Put(new Wall(pos + new Vector2Int(0, 1)));

    Put(new Ground(pos + new Vector2Int(1, 0)));
  }

  public void PlaceDownstairs(Vector2Int pos) {
    // surround sides with wall, but ensure left tile is open
    Put(new Ground(pos + new Vector2Int(-1, 0)));

    Put(new Wall(pos + new Vector2Int(0, -1)));
    Put(new Downstairs(pos));
    Put(new Wall(pos + new Vector2Int(0, 1)));

    Put(new Wall(pos + new Vector2Int(1, -1)));
    Put(new Wall(pos + new Vector2Int(1, 0)));
    Put(new Wall(pos + new Vector2Int(1, 1)));
  }

}