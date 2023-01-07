using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public partial class Floor {
  public readonly int width;
  public readonly int height;
  public int depth;
  private PathfindingManager pathfindingManager;
  private float lastStepTime = 0;
  // the time the player first entered the floor.
  public float timeBegan = -1;
  public float age => GameModel.main.time - timeBegan;

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

  internal Room root;
  /// all rooms (terminal bsp nodes). Sorted by 
#if !experimental_chainfloors
  [NonSerialized] /// not used beyond generator
#endif
  internal List<Room> rooms;
  // [NonSerialized] /// not used beyond generator
  internal Room startRoom => startTile?.room;
  [NonSerialized] /// not used beyond generator
  internal Room downstairsRoom;

  public Vector2Int startPos;
  public Tile startTile => this.tiles[startPos];

  // public Upstairs upstairs {
  //   get {
  //     foreach (Tile t in this.tiles) {
  //       if (t is Upstairs) {
  //         return (Upstairs)t;
  //       }
  //     }
  //     return null;
  //   }
  // }

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
    startPos = this.center;
  }

  public void BodyPlacementBehavior(Body body) {
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

  /// what should happen when the player goes downstairs
  internal virtual void PlayerGoDownstairs() {
    // if we're home, go back to the cave
    // if we're in the cave, go home
    int nextDepth;
#if experimental_actionpoints
    if (depth == 0) {
      nextDepth = GameModel.main.nextCaveDepth;
    } else {
      nextDepth = 0;
    }
#else
    if (depth == 0) {
      nextDepth = GameModel.main.cave.depth;
    } else {
      nextDepth = depth + 1;
    }
#endif
    if (nextDepth == 0) {
      PlayerGoHome();
    } else {
      GameModel.main.PutPlayerAt(nextDepth);
    }
#if experimental_retryondemand
    GameModel.main.DrainEventQueue();
    Serializer.SaveMainToLevelStart();
#endif
  }

  internal void PlayerGoHome() {
    Serializer.SaveMainToCheckpoint();
    // destroy player's inventory
    var inventoryItems = GameModel.main.player.inventory.ItemsNonNull();
    foreach (var item in inventoryItems) {
      // aaand it's just gone
      GameModel.main.player.inventory.RemoveItem(item);
      if (item is ItemOfPiece || item is ItemGrass || item is ItemPlaceableEntity || item is ItemBox) {
        // put pieces at home base automatically
        GameModel.main.home.Put(new ItemOnGround(GameModel.main.home.center, item));
      }
    }

    var equipmentItems = GameModel.main.player.equipment.ItemsNonNull();
    foreach (var item in equipmentItems) {
      GameModel.main.player.equipment.RemoveItem(item);
    }

    GameModel.main.player.Replenish();
    GameModel.main.PutPlayerAt(0);
    GameModel.main.GenerateAndSetCaveDepth(GameModel.main.nextCaveDepth + 1);
    GameModel.main.GoNextDay();
  }

  public int EnemiesLeft() {
    var enemyActors = Enemies();
    var enemyGrasses = grasses
      .Where(g => g.room == GameModel.main.player.room)
      .Where(g => g is IEnemyGrass);
    // return Enemies().Count();
    return enemyActors.Count() + enemyGrasses.Count();
  }

  public IEnumerable<AIActor> Enemies() {
    var enemies = bodies.Where(b => b is AIActor a && a.faction == Faction.Enemy).Cast<AIActor>();
#if experimental_chainfloors
    enemies = enemies.Where(a => a.room == GameModel.main.player.room);
#endif
    return enemies;
  }

  public virtual void Put(Entity entity) {
    try {
      this.entities.Add(entity);

      if (entity is Player && timeBegan < 0) {
        timeBegan = GameModel.main.time;
      }

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
        GameModel.main.EnqueueEvent(RecomputeVisibility);
      }

      entity.SetFloor(this);
      this.OnEntityAdded?.Invoke(entity);
    } catch (NoSpaceException) {
      Remove(entity);
    }
  }

  public virtual void Remove(Entity entity) {
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
      GameModel.main.EnqueueEvent(RecomputeVisibility);
    }

    entity.SetFloor(null);
    this.OnEntityRemoved?.Invoke(entity);

    if (entity is AIActor a && a.faction == Faction.Enemy || entity is IEnemyGrass) {
      CheckTeleporter();
    }
  }

  public void CheckTeleporter() {
    GameModel.main.EnqueueEvent(() => {
      var activeRoom = GameModel.main.player.room;
      var existingTeleporter = this.EnumerateRoom(activeRoom).Select(p => tiles[p]).OfType<Teleporter>().FirstOrDefault();
      if (EnemiesLeft() == 0 && existingTeleporter == null) {
        // create a teleporter
        var freeSpot = FloorUtils.TilesFromCenter(this, GameModel.main.player.room).FirstOrDefault();
        if (freeSpot == null) {
          freeSpot = GetAdjacentTiles(GameModel.main.player.pos)[0];
        }
        Put(new Teleporter(freeSpot.pos));
        CreateRewards().ShowUI();
        // new CaptureAction().ShowTargetingUIThenPerform(GameModel.main.player);
      }
    });
  }

  public List<Type> EntityTypes = new List<Type>();
  public void ComputeEntityTypes() {
    EntityTypes =
      bodies
      .Where(b => !(b is IHideInSidebar))
      .Select(b => b.GetType())
      .Concat(
        grasses
        .Where(g => !(g is IHideInSidebar))
        .Select(g => g.GetType())
      )
      .Distinct()
      .ToList();
  }

  public Rewards CreateRewards() {
    var rewards = new Rewards(EntityTypes.Select(type => {
      if (type.IsSubclassOf(typeof(Grass))) {
        return new Inventory(new ItemGrass(type, 1));
      } else if (type.IsSubclassOf(typeof(Body))) {
        var constructor = type.GetConstructor(new Type[] { typeof(Vector2Int) });
        var entity = (Entity) constructor.Invoke(new object[] { new Vector2Int(0, 0) });
        if (entity.GetHomeItem() == null) {
          return null;
        }
        if (entity is AIActor actor) {
          actor.SetAI(new WaitAI(actor));
          actor.statuses.Add(new CharmedStatus());
          actor.faction = Faction.Ally;
        }
        ItemPlaceableEntity item = new ItemPlaceableEntity(entity);
        return new Inventory(item);
      } else {
        return null;
      }
    }).Where(i => i != null).ToList());
    rewards.inventories.Add(new Inventory(new ItemPlaceableTile(typeof(HomeGround), 3)));
    return rewards;
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

  public virtual void RecomputeVisibility() {
    var player = GameModel.main.player;
    if (player == null || player.floor != this) {
      return;
    }

    foreach (var pos in this.EnumerateFloor()) {
      Tile t = tiles[pos.x, pos.y];
      if (t != null) {
        t.visibility = RecomputeVisibilityFor(t);
      }
    }
  }

  protected virtual TileVisiblity RecomputeVisibilityFor(Tile t) {
    var isEnclosedByWalls = GetAdjacentTiles(t.pos).All(t => t is Wall);
    if (isEnclosedByWalls) {
      // looks better when we skip fully enclosed Walls
      return t.visibility; // t.visibility = TileVisiblity.Unexplored;
    }

    var player = GameModel.main.player;

#if experimental_chainfloors
    var activeRoom = player.room;
    var tileRoom = t.room;
    bool isNextToActiveRoom = activeRoom == null ? true : (activeRoom.DistanceTo(t.pos) <= 1);
    bool isCrossingRooms = tileRoom != null && activeRoom != tileRoom;
    if (isCrossingRooms && !player.IsNextTo(t) && !isNextToActiveRoom) {
      return TileVisiblity.Unexplored;
    }
#endif

    var isCamouflaged = player.isCamouflaged;
    if (isCamouflaged) {
      t.visibility = t.pos == player.pos ? TileVisiblity.Visible : TileVisiblity.Explored;
      return t.visibility;
    }

    var newVisibility = TestVisibility(player.pos, t.pos);
    if (t.isExplored && newVisibility == TileVisiblity.Unexplored) {
      return t.visibility;
    }
    return newVisibility;
  }

  internal void ForceAddVisibility(IEnumerable<Vector2Int> positions = null) {
    if (positions == null) {
      positions = this.EnumerateRectangle(Vector2Int.zero, new Vector2Int(width, height));
    }
    foreach (var pos in positions) {
      Tile t = tiles[pos];
      t.visibility = TileVisiblity.Visible;
    }
  }

  private TileVisiblity TestVisibilityOneDir(Vector2Int source, Vector2Int end) {
    // tiles can always see themselves and adjacent neighbors; this is important if the player is standing on a fern
    // if (Util.DiamondMagnitude(source - end) <= 1) {
    //   return true;
    // }

    if (tiles[end.x, end.y].ObstructsExploration()) {
      var possibleEnds = GetAdjacentTiles(end).Where(tile => !tile.ObstructsExploration()).OrderBy((tile) => {
        return Vector2Int.Distance(source, tile.pos);
      });
      /// find the closest neighbor that doesn't obstruct vision and go off that
      end = possibleEnds.FirstOrDefault()?.pos ?? end;
    }
    foreach (var pos in this.EnumerateLine(source, end)) {
      if (pos == source || pos == end) {
        continue;
      }
      Tile t = tiles[pos.x, pos.y];
      if (t.ObstructsExploration()) {
        return TileVisiblity.Unexplored;
      }
      if (t.ObstructsVision()) {
        return TileVisiblity.Explored;
      }
    }
    return TileVisiblity.Visible;
  }

  /// returns true if the points have line of sight to each other
  public TileVisiblity TestVisibility(Vector2Int source, Vector2Int end) {
    // EnumerateLine is not commutative so make it
    return (TileVisiblity) Math.Max((int)TestVisibilityOneDir(source, end), (int)TestVisibilityOneDir(end, source));
  }

  /// includes the tile *at* the pos.
  public List<Tile> GetAdjacentTiles(Vector2Int pos) {
    List<Tile> list = new List<Tile>();
    int xMin = Mathf.Clamp(pos.x - 1, 0, width - 1);
    int xMax = Mathf.Clamp(pos.x + 1, 0, width - 1);
    int yMin = Mathf.Clamp(pos.y - 1, 0, height - 1);
    int yMax = Mathf.Clamp(pos.y + 1, 0, height - 1);
#if experimental_nodiagonalmovement
    list.Add(tiles[xMin, pos.y]);
    list.Add(tiles[pos.x, yMin]);
    list.Add(tiles[pos]);
    list.Add(tiles[pos.x, yMax]);
    list.Add(tiles[xMax, pos.y]);
#else
    for (int x = xMin; x <= xMax; x++) {
      for (int y = yMin; y <= yMax; y++) {
        list.Add(tiles[x, y]);
      }
    }
#endif
    return list;
  }

  public List<Tile> GetDiagonalAdjacentTiles(Vector2Int pos) {
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

  public void SetStartPos(Vector2Int pos, bool addHardGround = true) {
    // Put(new Upstairs(pos));
    startPos = pos;
    // surround with Hard Ground
    if (addHardGround) {
      var adjacentGrounds = GetAdjacentTiles(pos).Where(t => t is Ground).ToList();
      foreach (var ground in adjacentGrounds) {
        Put(new HardGround(ground.pos));
      }
    }
  }

  public void PlaceDownstairs(Vector2Int pos, bool addHardGround = true) {
    if (depth > 0) {
      Put(new Teleporter(pos));
    } else {
      Put(new Downstairs(pos));
    }
    // surround with Hard Ground
    if (addHardGround) {
      var adjacentGrounds = GetAdjacentTiles(pos).Where(t => t is Ground).ToList();
      foreach (var ground in adjacentGrounds) {
        Put(new HardGround(ground.pos));
      }
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
