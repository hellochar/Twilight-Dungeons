using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NeighborTileset {
  private Sprite[] coloredSprites;

  public static NeighborTileset WaterTiles = new NeighborTileset {
    [new int[3, 3] {
    { 0, 1, 0 },
    { 0, 1, 0 },
    { 0, 1, 0 }}] = "colored_200",

    [new int[3, 3] {
    {0, 0, 0},
    {0, 1, 1},
    {0, 1, 0}}] = "colored_201",

    [new int[3, 3] {
    {0, 1, 0},
    {0, 1, 1},
    {0, 1, 0}}] = "colored_202",

    [new int[3, 3] {
    {0, 1, 0},
    {1, 1, 1},
    {0, 1, 0}}] = "colored_203",

    [new int[3, 3] {
    {0, 0, 0},
    {0, 1, 0},
    {0, 1, 0}}] = "colored_204",

    [new int[3, 3] {
    {1, 1, 1},
    {1, 1, 1},
    {1, 1, 1}}] = "colored_248",

    [new int[3, 3] {
    {0, 1, 1},
    {0, 1, 1},
    {0, 1, 1}}] = "colored_249",

    [new int[3, 3] {
    {0, 0, 0},
    {0, 1, 1},
    {0, 1, 1}}] = "colored_250",

    [new int[3, 3] {
    {0, 1, 1},
    {1, 1, 1},
    {1, 1, 1}}] = "colored_251"
  };

  // key is a chunkstring, value is (texture name, rotation in degrees)
  private Dictionary<string, (Sprite, int)> dict = new Dictionary<string, (Sprite, int)>();

  public string this[int[,] chunk] {
    // get => dict.TryGetValue(ChunkToString(chunk), out var s) ? s.Item1 : null;
    set {
      /// chunks are specified with y0 being at the top; but we want y0 to be at the bottom. Rearrange the order
      var tmp = (chunk[0, 0], chunk[1, 0], chunk[2, 0]);
      (chunk[0, 0], chunk[1, 0], chunk[2, 0]) = (chunk[0, 2], chunk[1, 2], chunk[2, 2]);
      (chunk[0, 2], chunk[1, 2], chunk[2, 2]) = tmp;

      if (coloredSprites == null) {
        coloredSprites = Resources.LoadAll<Sprite>("colored");
      }

      var sprite = coloredSprites.First((s) => s.name == value);

      dict[ChunkToString(chunk)] = (sprite, 0);

      int[,] chunk90 = Util.Rotate90(chunk);
      dict[ChunkToString(chunk90)] = (sprite, 90);

      int[,] chunk180 = Util.Rotate90(chunk90);
      dict[ChunkToString(chunk180)] = (sprite, 180);

      int[,] chunk270 = Util.Rotate90(chunk180);
      dict[ChunkToString(chunk270)] = (sprite, 270);
    }
  }

  public bool TryGetSpriteAndRotationFor(int[,] chunk, out (Sprite, int) tuple) {
    if (dict.TryGetValue(ChunkToString(chunk), out tuple)) {
      return true;
    }
    return false;
  }

  public static void ApplyNeighborAwareTile(Water water, SpriteRenderer spriteRenderer) {
    var chunk = new int[3, 3];
    var floor = water.floor;
    Util.FillChunkCenteredAt(floor, water.pos.x, water.pos.y, ref chunk, (pos) => floor.tiles[pos] is Water ? 1 : 0, 0);
    if (NeighborTileset.WaterTiles.TryGetSpriteAndRotationFor(chunk, out var tuple)) {
      var (sprite, rotation) = tuple;
      spriteRenderer.sprite = sprite;

      var goRot = spriteRenderer.gameObject.transform.rotation;
      goRot.z = rotation;
      spriteRenderer.gameObject.transform.rotation = goRot;
    }
  }

  private static string ChunkToString(int[,] chunk) {
    var s = "";
    foreach (var i in chunk) {
      s += i.ToString();
    }
    return s;
  }
}
