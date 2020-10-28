using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Floor {
  public static readonly int WIDTH = 60;

  public static readonly int HEIGHT = 20;

  public Tile[,] tiles;

  public List<Entity> entities;

  public Floor() {
    this.tiles = new Tile[WIDTH, HEIGHT];
    this.entities = new List<Entity>();
  }

  public Tile upstairs {
    get {
      foreach (Tile t in this.tiles) {
        if (t is Upstairs) {
          return t;
        }
      }
      return null;
    }
  }
  public Tile downstairs {
    get {
      foreach (Tile t in this.tiles) {
        if (t is Downstairs) {
          return t;
        }
      }
      return null;
    }
  }

  public int width {
    get {
      return tiles.GetLength(0);
    }
  }

  public int height {
    get {
      return tiles.GetLength(1);
    }
  }

  /// returns a list of adjacent positions that form the path, or an empty list if no path is found
  internal List<Vector2Int> FindPath(Vector2Int pos, Vector2Int target) {
    float[,] tilesmap = new float[width, height];
    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        Tile tile = tiles[x, y];
        float weight = tile.GetPathfindingWeight();
        tilesmap[x, y] = weight;
      }
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

  public void ForEachLocation(System.Action<Vector2Int> callback) {
    for (int x = 0; x < this.width; x++) {
      for (int y = 0; y < this.height; y++) {
        callback(new Vector2Int(x, y));
      }
    }
  }

  public void ForEachLine(Vector2Int startPoint, Vector2Int endPoint, System.Action<Vector2Int> cb) {
    Vector2 offset = endPoint - startPoint;
    for (float t = 0; t <= offset.magnitude; t += 0.5f) {
      Vector2 point = startPoint + offset.normalized * t;
      Vector2Int p = new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
      cb(p);
    }
    cb(endPoint);
  }


  public static Floor generateFloor0() {
    Floor f = new Floor();

    // fill with floor tiles by default
    f.ForEachLocation(p => f.tiles[p.x, p.y] = new Ground(p));

    // surround floor with walls
    for (int x = 0; x < WIDTH; x++) {
      f.tiles[x, 0] = new Wall(new Vector2Int(x, 0));
      f.tiles[x, HEIGHT - 1] = new Wall(new Vector2Int(x, HEIGHT - 1));
    }
    for (int y = 0; y < HEIGHT; y++) {
      f.tiles[0, y] = new Wall(new Vector2Int(0, y));
      f.tiles[WIDTH - 1, y] = new Wall(new Vector2Int(WIDTH - 1, y));
    }

    // add an upstairs at (2, height/2)
    // add a downstairs a (width - 3, height/2)
    f.tiles[2, HEIGHT / 2] = new Upstairs(new Vector2Int(2, HEIGHT / 2));
    f.tiles[WIDTH - 3, HEIGHT / 2] = new Downstairs(new Vector2Int(WIDTH - 3, HEIGHT / 2));

    return f;
  }

  public static Floor generateRandomFloor() {
    Floor floor = new Floor();

    // fill with wall
    floor.ForEachLocation(p => floor.tiles[p.x, p.y] = new Wall(p));

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

    // draw a path connecting siblings together (guarantees connectedness)
    root.Traverse(node => {
      if (!node.isTerminal) {
        Vector2Int aCenter = node.split.Value.a.getCenter();
        Vector2Int bCenter = node.split.Value.b.getCenter();
        floor.ForEachLine(aCenter, bCenter, point => floor.tiles[point.x, point.y] = new Dirt(point));
      }
    });

    // fill each room with floor
    rooms.ForEach(room => {
      for (int x = room.min.x; x <= room.max.x; x++) {
        for (int y = room.min.y; y <= room.max.y; y++) {
          floor.tiles[x, y] = new Ground(new Vector2Int(x, y));
        }
      }
    });

    // add an upstairs at (2, height/2)
    // add a downstairs a (width - 3, height/2)
    floor.tiles[2, HEIGHT / 2] = new Upstairs(new Vector2Int(2, HEIGHT / 2));
    floor.tiles[WIDTH - 3, HEIGHT / 2] = new Downstairs(new Vector2Int(WIDTH - 3, HEIGHT / 2));

    return floor;
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

    this.min.x = startX;
    this.min.y = startY;

    // subtract 1 from width/height since max is inclusive
    this.max.x = startX + roomWidth - 1;
    this.max.y = startY + roomHeight - 1;
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
}