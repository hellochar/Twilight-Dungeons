using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public class Floor {
  public readonly int width;
  public readonly int height;
  public int depth;
  public bool isCleared { get; protected set; }
  private PathfindingManager pathfindingManager;
  private float lastStepTime = 0;

  /// TODO refactor this into "layers": Tile Layer, Floor Layer, Main layer.
  public StaticEntityGrid<Tile> tiles;
  public StaticEntityGrid<Grass> grasses;
  public StaticEntityGrid<ItemOnGround> items;
  public StaticEntityGrid<Trigger> triggers;
  public MovingEntityList<Body> bodies;
  public HashSet<Entity> entities;
  public List<Boss> bosses;
  public IEnumerable<Boss> seenBosses => bosses.Where(b => b.isSeen);
  public List<ISteppable> steppableEntities;

  [field:NonSerialized] /// controller only (for now)
  public event Action<Entity> OnEntityAdded;
  [field:NonSerialized] /// gameplay events are being manually re-registered on deserialization
  public event Action<Entity> OnEntityRemoved;

  /// min inclusive, max exclusive in terms of map width/height
  public Vector2Int boundsMin => Vector2Int.zero;
  public Vector2Int boundsMax => new Vector2Int(width, height);
  public Vector2Int center => new Vector2Int(width / 2, height / 2);

  /// abstract bsp root
  // [NonSerialized] /// not used beyond generator
  internal Room root;
  /// all rooms (terminal bsp nodes). Sorted by 
  // [NonSerialized] /// not used beyond generator
  internal List<Room> rooms;
  // [NonSerialized] /// not used beyond generator
  internal Room upstairsRoom;
  [NonSerialized] /// not used beyond generator
  internal Room downstairsRoom;

  public Vector2Int startPos;
  public Vector2Int upstairsPos => upstairs?.pos ?? startPos + Vector2Int.left;
  public Vector2Int downstairsPos;
  public float timePlayerEntered = 0;
  public bool isActive => GameModel.main.currentFloor == this;

  public float age => GameModel.main.time - timePlayerEntered;

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

  public Floor(int depth, int width, int height) {
    this.depth = depth;
    this.width = width;
    this.height = height;
    startPos = new Vector2Int(1, height / 2);
    downstairsPos = new Vector2Int(width - 1, height / 2);
    this.isCleared = false;
    this.lastStepTime = GameModel.main.time;
    this.tiles = new StaticEntityGrid<Tile>(this);
    this.grasses = new StaticEntityGrid<Grass>(this);
    this.items = new StaticEntityGrid<ItemOnGround>(this, ItemPlacementBehavior);
    this.triggers = new StaticEntityGrid<Trigger>(this);
    this.bodies = new MovingEntityList<Body>(this, BodyPlacementBehavior);
    this.entities = new HashSet<Entity>();
    this.bosses = new List<Boss>();
    this.steppableEntities = new List<ISteppable>();
    pathfindingManager = new PathfindingManager(this);
  }

  private void BodyPlacementBehavior(Body body) {
    var newPosition = this.BreadthFirstSearch(body.pos, (_) => true)
      .Where(tile => tile.CanBeOccupied())
      .FirstOrDefault();
    if (newPosition == null) {
      throw new NoSpaceException();
    } else {
      body.pos = newPosition.pos;
    }
  }

  private void ItemPlacementBehavior(ItemOnGround item) => ItemOnGround.PlacementBehavior(this, item);

  public void BodyMoved() => bodies.ScheduleRecompute();

  public Floor StartCleared() {
    // set isCleared without triggering any events
    isCleared = true;
    return this;
  }

  /// what should happen when the player goes downstairs
  internal virtual void PlayerGoDownstairs() {
    // if we're home, go back to the cave
    // if we're in the cave, go 1 deeper
    int nextDepth;
    if (depth == 0) {
      nextDepth = GameModel.main.cave.depth;
    } else {
      nextDepth = depth + 1;
    }
    Serializer.SaveMainToCheckpoint();
    GameModel.main.PutPlayerAt(nextDepth);
  }

  public int EnemiesLeft() {
    return Enemies().Count();
  }

  public IEnumerable<Entity> Enemies() {
    return bodies.Where(b => (b is AIActor a && a.faction == Faction.Enemy) || b is IEnemyEntity).Cast<Entity>().Concat(
      grasses.Where(g => g is IEnemyEntity)
    );
  }

  public virtual void Put(Entity entity) {
    try {
      this.entities.Add(entity);

      if (entity is ISteppable s) {
        steppableEntities.Add(s);
      }
      if (entity is Boss b) {
        bosses.Add(b);
      }

      if (entity is Tile tile) {
        tiles.Put(tile);
      } else if (entity is Body body) {
        bodies.Put(body);
      } else if (entity is Grass grass) {
        grasses.Put(grass);
      } else if (entity is ItemOnGround item) {
        items.Put(item);
      } else if (entity is Trigger t) {
        triggers.Put(t);
      }

      /// HACK
      if (entity is IBlocksVision) {
        RecomputeVisibility();
      }

      entity.SetFloor(this);
      this.OnEntityAdded?.Invoke(entity);
    } catch (NoSpaceException) {
      Remove(entity);
    }
  }

  public void Remove(Entity entity) {
    if (!entities.Contains(entity)) {
      Debug.LogError("Removing " + entity + " from a floor it doesn't live in!");
      return;
    } else {
      this.entities.Remove(entity);
    }

    if (entity is ISteppable s) {
      steppableEntities.Remove(s);
    }
    if (entity is Boss boss) {
      bosses.Remove(boss);
    }

    if (entity is Tile tile) {
      tiles.Remove(tile);
    } else if (entity is Body b) {
      bodies.Remove(b);
    } else if (entity is Grass g) {
      grasses.Remove(g);
    } else if (entity is ItemOnGround item) {
      items.Remove(item);
    } else if (entity is Trigger t) {
      triggers.Remove(t);
    }

    /// HACK
    if (entity is IBlocksVision) {
      RecomputeVisibility();
    }

    entity.SetFloor(null);
    this.OnEntityRemoved?.Invoke(entity);
    if (entity is AIActor a && a.faction == Faction.Enemy || entity is IEnemyEntity) {
      CheckCleared();
    }
  }

  public void CheckCleared() {
    if (isCleared) {
      return;
    }

    // MEGA hack do three level nesting so we make sure any entity creation things have happened first
    GameModel.main.EnqueueEvent(() => {
      GameModel.main.EnqueueEvent(() => {
        GameModel.main.EnqueueEvent(() => {
          bool shouldCountAsCleared = EnemiesLeft() == 0;
          // check isCleared again if it's changed in the event queue
          if (shouldCountAsCleared && !isCleared) {
            ClearFloor();
          }
        });
      });
    });
  }

  public virtual void ClearFloor() {
    isCleared = true;
    AddUpstairs();
    var isLastFloor = (depth + 1) == GameModel.main.generator.floorGenerators.Count();
    if (!isLastFloor) {
      AddDownstairs();
    }
    GameModel.main.FloorCleared(this);
  }

  public void AddUpstairs() {
    if (upstairs == null) {
      // create an Upstairs
      // var freeSpot = this.BreadthFirstSearch(startPos + Vector2Int.left).Where(t => t is Ground && t.CanBeOccupied()).FirstOrDefault();
      // if (freeSpot == null) {
      //   freeSpot = GameModel.main.player.tile;
      // }
      Put(new Upstairs(startPos + Vector2Int.left));
    }
  }

  public void AddDownstairs() {
    if (downstairs == null) {
      // create a Downstairs
      // var freeSpot = this.BreadthFirstSearch(pos.Value).Where(t => t is Ground && t.CanBeOccupied()).FirstOrDefault();
      // if (freeSpot == null) {
      //   freeSpot = GameModel.main.player.tile;
      // }
      // Put(new Downstairs(freeSpot.pos));
      Put(new Downstairs(downstairsPos));
    }
  }

  internal void PutAll(IEnumerable<Entity> entities) {
    foreach (var entity in entities) {
      Put(entity);
    }
  }

  internal void PutAll(params Entity[] entities) {
    foreach (var entity in entities) {
      Put(entity);
    }
  }

  internal void RemoveAll(IEnumerable<Entity> entities) {
    foreach (var entity in entities) {
      Remove(entity);
    }
  }

  internal void RecordLastStepTime(float time) {
    lastStepTime = time;
  }

  internal IEnumerable<Body> AdjacentBodies(Vector2Int pos) {
    return GetAdjacentTiles(pos).Select(x => x.body).Where(x => x != null);
  }

  internal IEnumerable<Actor> AdjacentActors(Vector2Int pos) {
    return GetAdjacentTiles(pos).Select(x => x.actor).Where(x => x != null);
  }

  internal List<Body> BodiesInCircle(Vector2Int center, float radius) {
    var bodies = new List<Body>();
    foreach (var pos in this.EnumerateCircle(center, radius)) {
      if (tiles[pos] != null && tiles[pos].body != null) {
        bodies.Add(tiles[pos].body);
      }
    }
    return bodies;
  }

  internal List<Actor> ActorsInCircle(Vector2Int center, float radius) {
    return BodiesInCircle(center, radius).Where(b => b is Actor).Cast<Actor>().ToList();
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

  public void RecomputeVisibility() {
    var player = GameModel.main.player;
    if (player == null || player.floor != this) {
      return;
    }

    var isCamouflaged = player.isCamouflaged;

    foreach (var pos in this.EnumerateFloor()) {
      Tile t = tiles[pos.x, pos.y];
      if (t is IAlwaysVisibleTile) {
        t.visibility = TileVisiblity.Visible;
        continue;
      }
      var isEnclosedByWalls = GetAdjacentTiles(pos).All(t => t is Wall);
      if (isEnclosedByWalls) {
        // looks better when we skip fully enclosed Walls
        continue; // t.visibility = TileVisiblity.Unexplored;
      }

      if (isCamouflaged) {
        t.visibility = pos == player.pos ? TileVisiblity.Visible : TileVisiblity.Explored;
        continue;
      }

      bool isVisible = TestVisibility(player.pos, pos);

      if (isVisible) {
        t.visibility = TileVisiblity.Visible;
      } else {
        t.visibility = TileVisiblity.Explored;
      }
    }
  }

  internal void ForceAddVisibility(IEnumerable<Vector2Int> positions = null) {
    if (positions == null) {
      positions = this.EnumerateFloor();
    }
    foreach (var pos in positions) {
      Tile t = tiles[pos];
      t.visibility = TileVisiblity.Visible;
    }
  }

  private bool TestVisibilityOneDir(Vector2Int source, Vector2Int end) {
    // tiles can always see themselves and adjacent neighbors; this is important if the player is standing on a fern
    // if (Util.DiamondMagnitude(source - end) <= 1) {
    //   return true;
    // }

    bool isVisible = true;
    // if (tiles[end.x, end.y].ObstructsVision()) {
    //   var possibleEnds = GetAdjacentTiles(end).Where(tile => !tile.ObstructsVision()).OrderBy((tile) => {
    //     return Vector2Int.Distance(source, tile.pos);
    //   });
    //   /// find the closest neighbor that doesn't obstruct vision and go off that
    //   end = possibleEnds.FirstOrDefault()?.pos ?? end;
    // }
    foreach (var pos in this.EnumerateLine(source, end)) {
      if (pos == source || pos == end) {
        continue;
      }
      Tile t = tiles[pos.x, pos.y];
      isVisible = isVisible && !t.ObstructsVision();
    }
    return isVisible;
  }

  /// returns true if the points have line of sight to each other
  public bool TestVisibility(Vector2Int source, Vector2Int end) {
    // EnumerateLine is not commutative so make it
    return TestVisibilityOneDir(source, end) || TestVisibilityOneDir(end, source);
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

  public IEnumerable<Tile> GetCardinalNeighbors(Vector2Int pos, bool includeSelf = false) {
    if (includeSelf) {
      yield return tiles[pos];
    }

    var up = pos + Vector2Int.up;
    if (InBounds(up)) {
      yield return tiles[up];
    }

    var right = pos + Vector2Int.right;
    if (InBounds(right)) {
      yield return tiles[right];
    }

    var down = pos + Vector2Int.down;;
    if (InBounds(down)) {
      yield return tiles[down];
    }

    var left = pos + Vector2Int.left;
    if (InBounds(left)) {
      yield return tiles[left];
    }
  }

  public bool InBounds(Vector2Int pos) {
    return pos.x >= 0 && pos.y >= 0 && pos.x < width && pos.y < height;
  }

  public void PlaceUpstairs(Vector2Int pos, bool addHardGround = true) {
    Put(new Upstairs(pos));
    // surround with Hard Ground
    if (addHardGround) {
      var adjacentGrounds = GetAdjacentTiles(pos).Where(t => t is Ground).ToList();
      foreach (var ground in adjacentGrounds) {
        Put(new HardGround(ground.pos));
      }
    }
  }

  public void PlaceDownstairs(Vector2Int pos) {
    downstairsPos = pos;
    Put(new Downstairs(pos));
    // surround with Hard Ground
    var adjacentGrounds = GetAdjacentTiles(pos).Where(t => t is Ground).ToList();
    foreach (var ground in adjacentGrounds) {
      Put(new HardGround(ground.pos));
    }
  }
}

// just a special marker - turn music off
[Serializable]
public class BossFloor : Floor {
  public BossFloor(int depth, int width, int height) : base(depth, width, height) {}
}

[Serializable]
public class NoSpaceException : Exception {
  public NoSpaceException() {
  }

  public NoSpaceException(string message) : base(message) {
  }

  public NoSpaceException(string message, Exception innerException) : base(message, innerException) {
  }

  protected NoSpaceException(SerializationInfo info, StreamingContext context) : base(info, context) {
  }
}
