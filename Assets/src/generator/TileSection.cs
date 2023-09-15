using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class TileSection {
  public Type[,] Types { get; }
  public readonly string source;
  public TileSection(Type[,] types, string source) {
    Types = types;
    this.source = source;
  }

  public int width => Types.GetLength(0);
  public int height => Types.GetLength(1);
  public Vector2Int Size => new Vector2Int(width, height);

    public TileSection Rot90() {
    // rotate 90 degrees clockwise
    var newTypes = new Type[height, width];
    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        newTypes[y, width - 1 - x] = Types[x, y];
      }
    }
    return new TileSection(newTypes, source);
  }

  public void Blit(Floor floor, Vector2Int topLeft) {
    var bottomLeft = topLeft - new Vector2Int(0, height - 1);
    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        var pos = bottomLeft + new Vector2Int(x, y);
        if (!floor.InBounds(pos)) {
          continue;
        }

        var tileType = Types[x, y];
        var constructor = tileType.GetConstructor(new Type[] { typeof(Vector2Int) });
        var tile = (Tile)constructor.Invoke(new object[] { pos });
        floor.Put(tile);
      }
    }
  }

  public static TileSection FromString(string source) {
    /* s will look like: @"
xx_
xxx
 x
"
    */
    source = source.Trim();
    if (source.Length == 0) {
      throw new Exception("Empty string!");
    }
    var lines = source.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

    // Text's coordinate system is +y down. Our game's coordinate system is +y up.
    // To account for this we flip the order of lines here.
    lines = lines.Reverse().ToArray();

    var width = lines.Select(line => line.Length).Max();
    var height = lines.Length;
    Type[,] Types = new Type[width, height];
    for(int y = 0; y < height; y++) {
      for (int x = 0; x < width; x++) {
        var line = lines[y];
        var curChar = line[x];
        Type tileType = null;
        switch (curChar) {
          case 'x':
            tileType = typeof(Wall);
            break;
          case 'c':
            tileType = typeof(Chasm);
            break;
          case '_':
          default:
            tileType = typeof(Ground);
            break;
        }
        Types[x, y] = tileType;
      }
    }
    return new TileSection(Types, source);
  }

  public static TileSection[] FromMultiString(string multiSections) {
    var sections = multiSections.Split(new string[] { Environment.NewLine + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
    return sections.Select(section => FromString(section.Trim())).ToArray();
  }

  internal static TileSection[] WithRotations(TileSection[] tileSections) {
    var rot90 = tileSections.Select(tileSection => tileSection.Rot90());
    var rot180 = rot90.Select(tileSection => tileSection.Rot90());
    var rot270 = rot180.Select(tileSection => tileSection.Rot90());
    return tileSections.Concat(rot90).Concat(rot180).Concat(rot270).ToArray();
  }
}

/**
  public static TileSection[] StandardSectionsSmall = FromMultiString(@"
xx_
xxx
_xx

xx
_x

_x_
xxx
_x_

_x_
xxx
__x

__x_
xxxx

xxxx
_x__

xxx
xx_

xx_
xxx

x__
xxx
x__
");

  public static TileSection[] StandardSections = FromMultiString(@"
xxx_
xxxx
xxx_
_xx_

_xxx_
xxxx_
_xxxx
__xx_

xxxx
xx_x
xxxx
_xxx

__xx
_xxx
xxxx
xxx_
xx__

_x_x_
xxxx_
_xxxx
__xxx

xx_xx
xxxx_
_xxxx
xx_xx

xx__
xxx_
xxxx
_xx_

====

xxx_
__xx
x_x_
xxxx

====

xxxx
x_xx
x__x
xxxx

====

xxxx_
x___x
x_x_x
xxxxx

====

xxx___
xxx___
___xxx
___xxx
___xx_

====

__xx__
_xxxx_
xxx_xx
xx__x_
_xxxx_

");
*/