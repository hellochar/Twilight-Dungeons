using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FloorGenerator {
  public static Floor generateFloor0() {
    Floor f = new Floor(30, 20);

    // fill with floor tiles by default
    f.ForEachLocation(p => f.tiles.Put(new Ground(p)));

    // surround floor with walls
    for (int x = 0; x < f.width; x++) {
      f.tiles.Put(new Wall(new Vector2Int(x, 0)));
      f.tiles.Put(new Wall(new Vector2Int(x, f.height - 1)));
    }
    for (int y = 0; y < f.height; y++) {
      f.tiles.Put(new Wall(new Vector2Int(0, y)));
      f.tiles.Put(new Wall(new Vector2Int(f.width - 1, y)));
    }

    SMOOTH_ROOM_EDGES.ApplyWithRotations(f);
    SMOOTH_WALL_EDGES.ApplyWithRotations(f);
    MAKE_WALL_BUMPS.ApplyWithRotations(f);

    // add an upstairs at (2, height/2)
    // add a downstairs a (width - 3, height/2)
    f.PlaceUpstairs(new Vector2Int(1, f.height / 2));
    f.PlaceDownstairs(new Vector2Int(f.width - 2, f.height / 2));

    f.AddActor(new BerryBush(new Vector2Int(f.width / 2, f.height / 2)));
    f.AddActor(new Bat(new Vector2Int(f.width / 3, f.height / 3)));
    f.AddActor(new Bat(new Vector2Int(f.width / 3, f.height / 3)));
    f.AddActor(new Bat(new Vector2Int(f.width / 3, f.height / 3)));
    f.AddActor(new Bat(new Vector2Int(f.width / 3, f.height / 3)));
    return f;
  }

  public static Floor generateRandomFloor() {
    Floor floor = new Floor(60, 20);

    // fill with wall
    floor.ForEachLocation(p => floor.tiles.Put(new Wall(p)));

    // randomly partition space into 20 rooms
    BSPNode root = new BSPNode(null, new Vector2Int(1, 1), new Vector2Int(floor.width - 2, floor.height - 2));
    for (int i = 0; i < 20; i++) {
      bool success = root.randomlySplit();
      if (!success) {
        Debug.Log("couldn't split at iteration " + i);
        break;
      }
    }

    // collect all rooms
    List<BSPNode> rooms = new List<BSPNode>();
    root.Traverse(node => {
      if (node.isTerminal) {
        rooms.Add(node);
      }
    });

    // shrink it into a subset of the space available; adds more 'emptiness' to allow
    // for tunnels
    rooms.ForEach(room => room.randomlyShrink());

    foreach (var (a, b) in ConnectRooms(rooms, root)) {
      floor.ForEachLine(a, b, point => floor.tiles.Put(new Ground(point)));
    }

    rooms.ForEach(room => {
      // fill each room with floor
      for (int x = room.min.x; x <= room.max.x; x++) {
        for (int y = room.min.y; y <= room.max.y; y++) {
          floor.tiles.Put(new Ground(new Vector2Int(x, y)));
        }
      }
    });

    // apply a natural look across the floor by smoothing both wall corners and space corners
    SMOOTH_ROOM_EDGES.ApplyWithRotations(floor);
    SMOOTH_WALL_EDGES.ApplyWithRotations(floor);
    MAKE_WALL_BUMPS.ApplyWithRotations(floor);

    // sort by distance to top-left.
    Vector2Int topLeft = new Vector2Int(0, floor.height);
    rooms.Sort((a, b) => {
      int aDist2 = Util.manhattanDistance(a.getTopLeft() - topLeft);
      int bDist2 = Util.manhattanDistance(b.getTopLeft() - topLeft);

      if (aDist2 < bDist2) {
        return -1;
      } else if (aDist2 > bDist2) {
        return 1;
      }
      return 0;
    });
    BSPNode upstairsRoom = rooms.First();
    // 1-px padding from the top-left of the room
    Vector2Int upstairsPos = new Vector2Int(upstairsRoom.min.x + 1, upstairsRoom.max.y - 1);
    floor.PlaceUpstairs(upstairsPos);

    BSPNode downstairsRoom = rooms.Last();
    // 1-px padding from the bottom-right of the room
    Vector2Int downstairsPos = new Vector2Int(downstairsRoom.max.x - 1, downstairsRoom.min.y + 1);
    floor.PlaceDownstairs(downstairsPos);

    floor.AddActor(new Bat(new Vector2Int(floor.width/2, floor.height / 2)));
    return floor;
  }

  /// Connect all the rooms together with at least one through-path
  static List<(Vector2Int, Vector2Int)> ConnectRooms(List<BSPNode> rooms, BSPNode root) {
    return ConnectRoomsBSPSiblings(rooms, root);
  }

  /// draw a path connecting siblings together, including intermediary nodes (guarantees connectedness)
  /// this tends to draw long lines that cut right through single thickness walls
  static List<(Vector2Int, Vector2Int)> ConnectRoomsBSPSiblings(List<BSPNode> rooms, BSPNode root) {
    List<(Vector2Int, Vector2Int)> paths = new List<(Vector2Int, Vector2Int)>();
    root.Traverse(node => {
      if (!node.isTerminal) {
        Vector2Int nodeCenter = node.getCenter();
        Vector2Int aCenter = node.split.Value.a.getCenter();
        Vector2Int bCenter = node.split.Value.b.getCenter();
        paths.Add((nodeCenter, aCenter));
        paths.Add((nodeCenter, bCenter));
      }
    });
    return paths;
  }

  static ShapeTransform SMOOTH_WALL_EDGES = new ShapeTransform(
    new int[3, 3] {
      {1, 1, 1},
      {0, 0, 1},
      {0, 0, 1},
    },
    1
  );

  static ShapeTransform SMOOTH_ROOM_EDGES = new ShapeTransform(
    new int[3, 3] {
      {0, 0, 0},
      {1, 1, 0},
      {1, 1, 0},
    },
    0
  );

  static ShapeTransform MAKE_WALL_BUMPS = new ShapeTransform(
    new int[3, 3] {
      {0, 0, 0},
      {1, 1, 1},
      {1, 1, 1},
    },
    0,
    // 50% chance to make a 2-run
    1 - Mathf.Sqrt(0.5f)
  // 0.33f
  );

  private FloorGenerator() {}
}

class ShapeTransform {
  /// Each number is a pathfinding weight (0 means infinity)
  public int[,] input;
  public int output;
  /// only do the transform this % of the time
  public float probability { get; }

  private ShapeTransform rot90;
  private ShapeTransform rot180;
  private ShapeTransform rot270;

  public ShapeTransform(int[,] input, int output, float probability = 1) {
    this.input = input;
    this.output = output;
    this.probability = probability;
  }

  /// Apply this transform to the floor. Accounts for all 4 rotations as well.
  public void ApplyWithRotations(Floor floor) {
    if (rot90 == null) {
      rot90 = new ShapeTransform(Rotate90(input), output, probability);
    }
    if (rot180 == null) {
      rot180 = new ShapeTransform(Rotate90(rot90.input), output, probability);
    }
    if (rot270 == null) {
      rot270 = new ShapeTransform(Rotate90(rot180.input), output, probability);
    }
    Apply(floor);
    rot90.Apply(floor);
    rot180.Apply(floor);
    rot270.Apply(floor);
    // ApplyChunkToFloor(floor);
    // List<(int, int)> placesToChange = getPlacesToChange(floor);
    // placesToChange.Concat(rot90.getPlacesToChange(floor));
    // placesToChange.Concat(rot180.getPlacesToChange(floor));
    // placesToChange.Concat(rot270.getPlacesToChange(floor));
    // ApplyPlacesToChange(floor, placesToChange);
  }

  /// Create a new chunk that's the old one rotated 90 degrees counterclockwise (because we're in a right-handed coordinate system)
  private int[,] Rotate90(int[,] chunk) {
    int i1 = chunk[0, 0];
    int i2 = chunk[0, 1];
    int i3 = chunk[0, 2];

    int i4 = chunk[1, 0];
    int i5 = chunk[1, 1];
    int i6 = chunk[1, 2];

    int i7 = chunk[2, 0];
    int i8 = chunk[2, 1];
    int i9 = chunk[2, 2];

    return new int[3, 3] {
      {i3, i6, i9},
      {i2, i5, i8},
      {i1, i4, i7}
    };
  }

  private void Apply(Floor floor) {
    ApplyPlacesToChange(floor, getPlacesToChange(floor));
  }

  private void ApplyPlacesToChange(Floor floor, List<(int, int)> placesToChange) {
    foreach (var (x, y) in placesToChange) {
      if (Random.value <= this.probability) {
        ApplyOutputAt(floor, x, y, output);
      }
    }
  }

  private List<(int, int)> getPlacesToChange(Floor floor) {
    int[,] chunk = new int[3, 3];
    List<(int, int)> placesToChange = new List<(int, int)>();
    for (int x = 1; x < floor.width - 1; x++) {
      for (int y = 1; y < floor.height - 1; y++) {
        // iterate through every 3x3 block and try to match 
        Fill3x3CenteredAt(floor, x, y, ref chunk);

        if (ChunkEquals(chunk, input)) {
          placesToChange.Add((x, y));
        }
      }
    }
    return placesToChange;
  }

  /// We turn each tile into its pathfinding weight
  private void Fill3x3CenteredAt(Floor floor, int x, int y, ref int[,] chunk) {
    for (int dx = -1; dx <= 1; dx++) {
      for (int dy = -1; dy <= 1; dy++) {
        chunk[dx + 1, dy + 1] = (int)floor.tiles[x + dx, y + dy].GetPathfindingWeight();
      }
    }
  }

  private bool ChunkEquals(int[,] a, int[,] b) {
    return a.Cast<int>().SequenceEqual(b.Cast<int>());
  }

  /// only construct new tiles if the weight is different
  private void ApplyOutputAt(Floor floor, int x, int y, int newValue) {
    // HACK - convert chunks to tiles: 0 -> Wall, 1 -> Ground
    Vector2Int pos = new Vector2Int(x, y);
    Tile currentTile = floor.tiles[pos.x, pos.y];
    if (currentTile.GetPathfindingWeight() != newValue) {
      Tile newTile;
      if (newValue == 0) {
        newTile = new Wall(pos);
      } else {
        newTile = new Ground(pos);
      }
      floor.tiles.Put(newTile);
    }
  }
}

enum SplitDirection { Vertical, Horizontal }

struct BSPSplit {

  public BSPSplit(BSPNode a, BSPNode b, SplitDirection direction, int coordinate) {
    this.a = a;
    this.b = b;
    this.direction = direction;
    this.coordinate = coordinate;
  }
  public BSPNode a { get; }
  public BSPNode b { get; }
  public SplitDirection direction { get; }
  public int coordinate { get; }
}

class BSPNode {
  /// rooms are at least 3x3
  public readonly static int MIN_ROOM_SIZE = 3;

  /// max is inclusive
  public Vector2Int min, max;

  public BSPSplit? split;
  public BSPNode parent;

  public bool isTerminal {
    get {
      return this.split == null;
    }
  }

  public BSPNode(BSPNode parent, Vector2Int min, Vector2Int max) {
    this.parent = parent;
    this.min = min;
    this.max = max;
  }

  public int width {
    get {
      // add one because max is inclusive
      return max.x - min.x + 1;
    }
  }

  public int height {
    get {
      // add one because max is inclusive
      return max.y - min.y + 1;
    }
  }

  public void randomlyShrink() {
    if (!this.isTerminal) {
      throw new System.Exception("Tried shinking a non-terminal BSPNode.");
    }
    // randomly decide a new width and height that's within the alloted space
    // 5
    int roomWidth = Random.Range(BSPNode.MIN_ROOM_SIZE, width + 1);
    int roomHeight = Random.Range(BSPNode.MIN_ROOM_SIZE, height + 1);

    // min.x = 1, max.x = 5, 5 - 5 + 1 = 1
    int startX = Random.Range(min.x, max.x - roomWidth + 1);
    int startY = Random.Range(min.y, max.y - roomHeight + 1);

    this.min = new Vector2Int(startX, startY);

    // subtract 1 from width/height since max is inclusive
    this.max = new Vector2Int(startX + roomWidth - 1, startY + roomHeight - 1);
  }

  public bool randomlySplit() {
    if (this.isTerminal) {
      return this.doSplit();
    } else {
      // randomly pick a child and split it. If not successful, try the other one.
      BSPNode a = this.split.Value.a;
      BSPNode b = this.split.Value.b;

      var (firstChoice, secondChoice) = Random.value < 0.5 ? (a, b) : (b, a);

      if (firstChoice.randomlySplit()) {
        return true;
      } else {
        return secondChoice.randomlySplit();
      }
    }
  }

  private bool canSplitVertical {
    get {
      // to split a room, we'd need at minimum for each room to be the split size, plus 1 unit space between them
      return height >= (MIN_ROOM_SIZE * 2 + 1);
    }
  }

  private bool canSplitHorizontal {
    get {
      return width >= (MIN_ROOM_SIZE * 2 + 1);
    }
  }

  private bool canSplit {
    get {
      if (isTerminal) {
        return canSplitVertical || canSplitHorizontal;
      }
      return isTerminal && (canSplitVertical || canSplitHorizontal);
    }
  }

  private bool doSplit() {
    if (!this.isTerminal) {
      throw new System.Exception("Attempted to call doSplit() on a BSPNode that is already split!");
    }
    // we are too small of a room; exit
    if (!canSplitVertical && !canSplitHorizontal) {
      return false;
    } else if (canSplitVertical && !canSplitHorizontal) {
      doSplitVertical();
      return true;
    } else if (!canSplitVertical && canSplitHorizontal) {
      doSplitHorizontal();
      return true;
    } else {
      // last case - both are possible
      if (Random.value < 0.5) {
        doSplitHorizontal();
      } else {
        doSplitVertical();
      }
      return true;
    }
  }

  private void doSplitHorizontal() {
    // e.g. range is [0, 11]. We can split anywhere from 3-8
    int splitMax = max.x - MIN_ROOM_SIZE;
    int splitMin = min.x + MIN_ROOM_SIZE;
    int splitPoint = Random.Range(splitMin, splitMax);
    BSPNode a = new BSPNode(this, this.min, new Vector2Int(splitPoint - 1, this.max.y));
    BSPNode b = new BSPNode(this, new Vector2Int(splitPoint + 1, this.min.y), this.max);
    this.split = new BSPSplit(a, b, SplitDirection.Horizontal, splitPoint);
  }

  private void doSplitVertical() {
    int splitMax = max.y - MIN_ROOM_SIZE;
    int splitMin = min.y + MIN_ROOM_SIZE;
    int splitPoint = Random.Range(splitMin, splitMax);
    BSPNode a = new BSPNode(this, this.min, new Vector2Int(this.max.x, splitPoint - 1));
    BSPNode b = new BSPNode(this, new Vector2Int(this.min.x, splitPoint + 1), this.max);
    this.split = new BSPSplit(a, b, SplitDirection.Vertical, splitPoint);
  }

  public void Traverse(System.Action<BSPNode> action) {
    action(this);
    if (!this.isTerminal) {
      this.split.Value.a.Traverse(action);
      this.split.Value.b.Traverse(action);
    }
  }

  internal Vector2Int getCenter() {
    return (this.max + this.min) / 2;
  }

  internal Vector2Int getTopLeft() {
    return new Vector2Int(this.min.x, this.max.y);
  }
}