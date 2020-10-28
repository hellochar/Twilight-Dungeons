using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameModel {
  public Player player;
  public Floor[] floors;
  public int activeFloorIndex = 0;

  public static GameModel model = GameModel.generateGameModel(); //new GameModel();
  public static GameModel generateGameModel() {
    GameModel model = new GameModel();
    model.floors = new Floor[] {
      Floor.generateFloor0(),
      Floor.generateRandomFloor(),
      Floor.generateRandomFloor(),
      Floor.generateRandomFloor(),
      Floor.generateRandomFloor(),
    };

    Tile floor0Upstairs = model.floors[0].upstairs;
    model.player = new Player(new Vector2Int(floor0Upstairs.pos.x + 1, floor0Upstairs.pos.y));
    model.floors[0].entities.Add(model.player);
    return model;
  }
}

public class Entity {
  public Vector2Int pos;
  public Entity(Vector2Int pos) {
    this.pos = pos;
  }
}

public class Player : Entity {
  public Player(Vector2Int pos) : base(pos) {

  }
}

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


  public static Floor generateFloor0() {
    Floor f = new Floor();

    // fill with floor tiles by default
    for (int x = 0; x < f.width; x++) {
      for (int y = 0; y < f.height; y++) {
        f.tiles[x, y] = new Ground(new Vector2Int(x, y));
      }
    }

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
    return generateFloor0();
  }
}

public abstract class Tile {
  public readonly Vector2Int pos;
  public Tile(Vector2Int pos) {
    this.pos = pos;
  }

  /// 0.0 means unwalkable.
  /// weight 1 is "normal" weight.
  public virtual float GetPathfindingWeight() {
    return 1;
  }
}

public class Ground : Tile {
  public Ground(Vector2Int pos) : base(pos) { }
}

public class Wall : Tile {
  public Wall(Vector2Int pos) : base(pos) { }
  public override float GetPathfindingWeight() {
    return 0;
  }
}

public class Upstairs : Tile {
  public Upstairs(Vector2Int pos) : base(pos) {
  }
}

public class Downstairs : Tile {
  public Downstairs(Vector2Int pos) : base(pos) { }
}