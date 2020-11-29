using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
