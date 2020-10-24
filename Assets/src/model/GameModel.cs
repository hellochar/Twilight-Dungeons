using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModel
{
  public Floor[] floors;
}

public class Floor
{
  public static readonly int WIDTH = 60;
  public static readonly int HEIGHT = 20;

  public Tile[,] tiles;

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

  public static Floor generateRandomLevel()
  {
    Floor l = new Floor();
    l.tiles = new Tile[WIDTH, HEIGHT];

    // add floor tiles randomly
    for (int x = 0; x < 10; x++)
    {
      int px = Random.Range(0, WIDTH);
      int py = Random.Range(0, HEIGHT);
      l.tiles[px, py] = new Ground(new Vector2Int(px, py));
    }

    // surround room with walls
    for (int x = 0; x < WIDTH; x++)
    {
      l.tiles[x, 0] = new Wall(new Vector2Int(x, 0));
      l.tiles[x, HEIGHT - 1] = new Wall(new Vector2Int(x, HEIGHT - 1));
    }
    for (int y = 0; y < HEIGHT; y++)
    {
      l.tiles[0, y] = new Wall(new Vector2Int(0, y));
      l.tiles[WIDTH - 1, y] = new Wall(new Vector2Int(WIDTH - 1, y));
    }
    return l;
  }
}

public abstract class Tile
{
  public readonly Vector2Int pos;
  public Tile(Vector2Int pos)
  {
    this.pos = pos;
  }
}

public class Ground : Tile
{
  public Ground(Vector2Int pos) : base(pos) { }
}

public class Wall : Tile
{
  public Wall(Vector2Int pos) : base(pos) { }
}